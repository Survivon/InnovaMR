﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Configuration
{
    using Microsoft.Bot.Configuration.Encryption;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public class TelegramSetting
    {
        [JsonProperty("telegramapikey")]
        public string BotKey { get; set; }

        [JsonProperty("telegramwebhook")]
        public string WebhookUrl { get; set; }
    }

    /// <summary>
    /// MrConfigurationManager represents configuration information for a bot.
    /// </summary>
    /// <remarks>It is typically loaded from a .bot file on disk.
    /// This class implements methods for encrypting and manipulating the in memory representation of the configuration.</remarks>
    public class MrConfigurationManager
    {
        private const string SECRETKEY = "secretKey";

        /// <summary>
        /// Gets or sets name of the bot.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets description of the bot.
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets padlock - Used to validate that the secret is consistent for all encrypted fields.
        /// </summary>
        [JsonProperty("padlock")]
        public string Padlock { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; } = "2.0";

        /// <summary>
        /// Gets or sets connected services.
        /// </summary>
        [JsonProperty("services")]
        [JsonConverter(typeof(BotServiceConverter))]
        public List<ConnectedService> Services { get; set; } = new List<ConnectedService>();

        [JsonProperty("telegramSetting")]
        public TelegramSetting TelegramSetting { get; set; }

        /// <summary>
        /// Gets or sets properties that are not otherwise defined.
        /// </summary>
        /// <value>The extended properties for the object.</value>
        /// <remarks>With this, properties not represented in the defined type are not dropped when
        /// the JSON object is deserialized, but are instead stored in this property. Such properties
        /// will be written to a JSON object when the instance is serialized.</remarks>
        [JsonExtensionData(ReadData = true, WriteData = true)]
        public JObject Properties { get; set; } = new JObject();

        /// <summary>
        /// Gets or sets the location of the configuration.
        /// </summary>
        [JsonIgnore]
        private string Location { get; set; }

        /// <summary>
        /// Load the bot configuration by looking in a folder and loading the first .bot file in the folder.
        /// </summary>
        /// <param name="folder">Folder to look for bot files. </param>
        /// <param name="secret">Secret to use to encrypt keys. </param>
        /// <returns><see cref="Task"/> of <see cref="MrConfigurationManager"/>.</returns>
        public static async Task<MrConfigurationManager> LoadFromFolderAsync(string folder, string secret = null)
        {
            if (string.IsNullOrEmpty(folder))
            {
                throw new ArgumentNullException(nameof(folder));
            }

            var file = Directory.GetFiles(folder, "*.bot", SearchOption.TopDirectoryOnly).FirstOrDefault();

            if (file != null)
            {
                return await MrConfigurationManager.LoadAsync(file, secret).ConfigureAwait(false);
            }

            throw new FileNotFoundException($"Error: no bot file found in {folder}. Choose a different location or use msbot init to create a.bot file.");
        }

        /// <summary>
        /// Load the bot configuration by looking in a folder and loading the first .bot file in the folder.
        /// </summary>
        /// <param name="folder">Folder to look for bot files. </param>
        /// <param name="secret">Secret to use to encrypt keys. </param>
        /// <returns><see cref="MrConfigurationManager"/>.</returns>
        public static MrConfigurationManager LoadFromFolder(string folder, string secret = null)
        {
            if (string.IsNullOrEmpty(folder))
            {
                throw new ArgumentNullException(nameof(folder));
            }

            return MrConfigurationManager.LoadFromFolderAsync(folder, secret).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Load the configuration from a .bot file.
        /// </summary>
        /// <param name="file">Path to bot file. </param>
        /// <param name="secret">Secret to use to decrypt the file on disk. </param>
        /// <returns><see cref="Task"/> of <see cref="MrConfigurationManager"/>.</returns>
        public static async Task<MrConfigurationManager> LoadAsync(string file, string secret = null)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException(nameof(file));
            }

            string json = string.Empty;
            using (var stream = File.OpenText(file))
            {
                json = await stream.ReadToEndAsync().ConfigureAwait(false);
            }

            var bot = JsonConvert.DeserializeObject<MrConfigurationManager>(json);
            bot.Location = file;
            bot.MigrateData();

            var hasSecret = bot.Padlock?.Length > 0;
            if (hasSecret)
            {
                bot.Decrypt(secret);
            }

            return bot;
        }

        /// <summary>
        /// Load the configuration from a .bot file.
        /// </summary>
        /// <param name="file">Path to bot file. </param>
        /// <param name="secret">Secret to use to decrypt the file on disk. </param>
        /// <returns><see cref="MrConfigurationManager"/>.</returns>
        public static MrConfigurationManager Load(string file, string secret = null)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException(nameof(file));
            }

            return MrConfigurationManager.LoadAsync(file, secret).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Generate a new key suitable for encrypting.
        /// </summary>
        /// <returns>key to use with <see cref="Encrypt(string)"/> method. </returns>
        public static string GenerateKey()
        {
            return EncryptUtilities.GenerateKey();
        }

        /// <summary>
        /// Save the file with secret.
        /// </summary>
        /// <param name="secret">Secret for encryption. </param>
        /// <returns><see cref="Task"/>.</returns>
        public Task SaveAsync(string secret = null)
        {
            return this.SaveAsAsync(this.Location, secret);
        }

        /// <summary>
        /// Save the file with secret.
        /// </summary>
        /// <param name="secret">Secret for encryption. </param>
        public void Save(string secret = null)
        {
            this.SaveAsync(secret).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Save the configuration to a .bot file.
        /// </summary>
        /// <param name="path">Path to bot file.</param>
        /// <param name="secret">Secret for encrypting the file keys.</param>
        /// <returns>Task. </returns>
        public async Task SaveAsAsync(string path = null, string secret = null)
        {
            // Validate state: Either path needs to be provided or Location needs to be set
            if (string.IsNullOrEmpty(path) && string.IsNullOrEmpty(this.Location))
            {
                // If location is not set, we expect the path to be provided
                throw new ArgumentException(nameof(path));
            }

            if (!string.IsNullOrEmpty(secret))
            {
                this.ValidateSecret(secret);
            }

            var hasSecret = this.Padlock?.Length > 0;

            // Make sure that all dispatch serviceIds still match services that are in the bot
            foreach (var dispatchService in this.Services.Where(s => s.Type == ServiceTypes.Dispatch).Cast<DispatchService>())
            {
                dispatchService.ServiceIds = dispatchService.ServiceIds
                        .Where(serviceId => this.Services.Any(s => s.Id == serviceId))
                        .ToList();
            }

            if (hasSecret)
            {
                // Make sure fields are encrypted before serialization
                this.Encrypt(secret);
            }

            // Save it to disk
            using (var file = File.Open(path ?? this.Location, FileMode.Create))
            {
                using (var textWriter = new StreamWriter(file))
                {
                    await textWriter.WriteLineAsync(JsonConvert.SerializeObject(this, Formatting.Indented)).ConfigureAwait(false);
                }
            }

            if (hasSecret)
            {
                // Make sure all in memory fields are decrypted again for continued operations
                this.Decrypt(secret);
            }
        }

        /// <summary>
        /// Save the configuration to a .bot file.
        /// </summary>
        /// <param name="path">Path to bot file.</param>
        /// <param name="secret">Secret for encrypting the file keys.</param>
        public void SaveAs(string path, string secret = null)
        {
            // Validate state: Either path needs to be provided or Location needs to be set
            if (string.IsNullOrEmpty(path) && string.IsNullOrEmpty(this.Location))
            {
                // If location is not set, we expect the path to be provided
                throw new ArgumentException(nameof(path));
            }

            this.SaveAsAsync(path, secret).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Clear secret.
        /// </summary>
        public void ClearSecret()
        {
            this.Padlock = string.Empty;
        }

        /// <summary>
        /// Connect a service to the bot file.
        /// </summary>
        /// <param name="newService"><see cref="ConnectedService"/> to add.</param>
        public void ConnectService(ConnectedService newService)
        {
            if (newService == null)
            {
                throw new ArgumentNullException(nameof(newService));
            }

            if (this.Services.Where(s => s.Type == newService.Type && s.Id == newService.Id).Any())
            {
                throw new Exception($"service with {newService.Id} is already connected");
            }
            else
            {
                // Assign a unique random id between 0-255 (255 services seems like a LOT of services
                var rnd = new Random();
                do
                {
                    newService.Id = rnd.Next(byte.MaxValue).ToString();
                }
                while (this.Services.Where(s => s.Id == newService.Id).Any());

                this.Services.Add(newService);
            }
        }

        /// <summary>
        /// Encrypt all values in the in memory config.
        /// </summary>
        /// <param name="secret">Secret to encrypt.</param>
        public void Encrypt(string secret)
        {
            this.ValidateSecret(secret);

            foreach (var service in this.Services)
            {
                service.Encrypt(secret);
            }
        }

        /// <summary>
        /// Decrypt all values in the in memory config.
        /// </summary>
        /// <param name="secret">Secret to encrypt.</param>
        public void Decrypt(string secret)
        {
            this.ValidateSecret(secret);

            foreach (var service in this.Services)
            {
                service.Decrypt(secret);
            }
        }

        /// <summary>
        /// Find service by name or id.
        /// </summary>
        /// <param name="nameOrId">Name or service id.</param>
        /// <returns>Found <see cref="ConnectedService"/>.</returns>
        public ConnectedService FindServiceByNameOrId(string nameOrId)
        {
            if (string.IsNullOrEmpty(nameOrId))
            {
                throw new ArgumentNullException(nameof(nameOrId));
            }

            return this.Services.FirstOrDefault(s => s.Id == nameOrId || s.Name == nameOrId);
        }

        /// <summary>
        /// Find a service by id.
        /// </summary>
        /// <param name="id">Id of the service.</param>
        /// <returns><see cref="ConnectedService"/>.</returns>
        public ConnectedService FindService(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            return this.Services.FirstOrDefault(s => s.Id == id);
        }

        /// <summary>
        /// Remove service by name or id.
        /// </summary>
        /// <param name="nameOrId">Name or service id.</param>
        /// <returns>Found <see cref="ConnectedService"/>.</returns>
        public ConnectedService DisconnectServiceByNameOrId(string nameOrId)
        {
            if (string.IsNullOrEmpty(nameOrId))
            {
                throw new ArgumentNullException(nameof(nameOrId));
            }

            var service = this.FindServiceByNameOrId(nameOrId);
            if (service == null)
            {
                throw new Exception($"a service with id or name of[{nameOrId}] was not found");
            }

            this.Services.Remove(service);
            return service;
        }

        /// <summary>
        /// Remove a service by id.
        /// </summary>
        /// <param name="id">Id of the service.</param>
        public void DisconnectService(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            var service = this.FindService(id);
            if (service != null)
            {
                this.Services.Remove(service);
            }
        }

        /// <summary>
        /// Make sure secret is correct by decrypting the secretKey with it.
        /// </summary>
        /// <param name="secret">Secret to use.</param>
        protected void ValidateSecret(string secret)
        {
            if (secret?.Length == null)
            {
                throw new Exception("You are attempting to perform an operation which needs access to the secret and --secret is missing");
            }

            try
            {
                if (this.Padlock?.Length == 0)
                {
                    // If no key, create a guid and enrypt that to use as secret validator.
                    this.Padlock = Guid.NewGuid().ToString("n").Encrypt(secret);
                }
                else
                {
                    // This will throw exception if invalid secret.
                    this.Padlock.Decrypt(secret);
                }
            }
            catch
            {
                throw new Exception("You are attempting to perform an operation which needs access to the secret and --secret is incorrect.");
            }
        }

        /// <summary>
        /// migrate old records to new records.
        /// </summary>
        protected virtual void MigrateData()
        {
            // migrate old secretKey
            string secretKey = (string)this.Properties[SECRETKEY];
            if (secretKey != null)
            {
                if (this.Padlock == null)
                {
                    this.Padlock = secretKey;
                }

                this.Properties.Remove(SECRETKEY);
            }

            foreach (var service in this.Services)
            {
                switch (service.Type)
                {
                    case ServiceTypes.Bot:
                        {
                            var botService = (BotService)service;

                            // old bot service records may not have the appId on the bot, but we probably have it already on an endpoint
                            if (string.IsNullOrEmpty(botService.AppId))
                            {
                                botService.AppId = this.Services.Where(s => s.Type == ServiceTypes.Endpoint).Cast<EndpointService>()
                                    .Where(ep => !string.IsNullOrEmpty(ep.AppId))
                                    .Select(ep => ep.AppId)
                                    .FirstOrDefault();
                            }
                        }

                        break;

                    default:
                        break;
                }
            }

            // this is now a 2.0 version of the schema
            this.Version = "2.0";
        }

        /// <summary>
        /// Converter for strongly typed connected services.
        /// </summary>
        internal class BotServiceConverter : JsonConverter
        {
            public override bool CanWrite
            {
                get { return false; }
            }

            /// <summary>
            /// Checks whether the connected service can be converted to the provided type.
            /// </summary>
            /// <param name="objectType">Type to be checked for conversion. </param>
            /// <returns>Whether the connected service can be converted to the provided type.</returns>
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(List<ConnectedService>);
            }

            /// <inheritdoc/>
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                List<ConnectedService> services = new List<ConnectedService>();
                JArray array = JArray.Load(reader);
                foreach (JToken token in array)
                {
                    string type = token.Value<string>("type");
                    switch (type)
                    {
                        case ServiceTypes.Bot:
                            services.Add(token.ToObject<BotService>());
                            break;
                        case ServiceTypes.AppInsights:
                            services.Add(token.ToObject<AppInsightsService>());
                            break;
                        case ServiceTypes.BlobStorage:
                            services.Add(token.ToObject<BlobStorageService>());
                            break;
                        case ServiceTypes.CosmosDB:
                            services.Add(token.ToObject<CosmosDbService>());
                            break;
                        case ServiceTypes.Dispatch:
                            services.Add(token.ToObject<DispatchService>());
                            break;
                        case ServiceTypes.Endpoint:
                            services.Add(token.ToObject<EndpointService>());
                            break;
                        case ServiceTypes.File:
                            services.Add(token.ToObject<FileService>());
                            break;
                        case ServiceTypes.Luis:
                            services.Add(token.ToObject<LuisService>());
                            break;
                        case ServiceTypes.QnA:
                            services.Add(token.ToObject<QnAMakerService>());
                            break;
                        case ServiceTypes.Generic:
                            services.Add(token.ToObject<GenericService>());
                            break;
                        default:
                            System.Diagnostics.Trace.TraceWarning($"Unknown service type {type}");
                            services.Add(token.ToObject<ConnectedService>());
                            break;
                    }
                }

                return services;
            }

            /// <inheritdoc/>
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
