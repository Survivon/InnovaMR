using System;
using System.Collections.Generic;
using System.Threading;
using InnovaMRBot.Helpers;
using Xunit;
using InnovaMRBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using InnovaMRBot.Models;
using Newtonsoft.Json.Linq;

namespace InnovaMRBotTest
{
    public class UnitTest1
    {
        private const string CONVERSATION_KEY = "conversation";

        private const string USER_SETTING_KEY = "users";

        [Fact]
        public async void Test1()
        {
            IStorage storage = new MemoryStorage();
            var mrChatId = Guid.NewGuid().ToString();
            var alertChatId = Guid.NewGuid().ToString();

            await storage.WriteAsync(new Dictionary<string, object>()
            {
                { CONVERSATION_KEY, JObject.FromObject(new Conversations()
                {
                    BotConversation = new List<ConversationSetting>()
                    {
                        new ConversationSetting()
                        {
                            MRChat = new ChatSetting()
                            {
                                Id = mrChatId,
                                Name = "MR Chat",
                                IsMRChat = true
                            },
                            AlertChat = new ChatSetting()
                            {
                                Id = alertChatId,
                                Name = "Alert Chat",
                                IsAlertChat = true
                            },
                            ListOfMerge = new List<MergeSetting>(),
                        },
                    },
                }, new JsonSerializer(){TypeNameHandling = TypeNameHandling.All }) }
            }, CancellationToken.None);

            var state = new CustomConversationState(storage);

            var service = new ChatStateService(state);

            ITurnContext context = new TurnContext(new TestAdapter(), new Activity()
            {
                Text = @"http://gitlab.fortia.fr/Fortia/Innova/merge_requests/5286
https://fortia.atlassian.net/browse/INOCB-1892
https://fortia.atlassian.net/browse/INOCB-1989
���� ��������� ���������� ���������� �� ICCP ������. ����� 1/3.
����� 2 � 3 ����� ����������� � ���� ����� ��������. Obsolete ��� �������� ����������� ����� ����, ������� ����� ���� ������������ � ����������

�����������:
1. ��������� ������� �� Genius ������ � 3 ����������� ����������
void StartParsing();
void ApplySimilarRules();
void SimulateRule();
2. �� ����������� ������:
The Jointure is assigned to the selected Rule Affectation Tasks when clicking on Apply
If None is selected. The jointure is unassigned from the selected tasks
The Start Date/ End Date is applied to the selected Rule Affectation Tasks when clicking on Apply to the selected task(s)
It is possible to have an empty Start/End date
Nothing is saved in the database until the user clicks on Save.
3. �������� ��� �������� � �����������",
                Type = ActivityTypes.Message,
                Conversation = new ConversationAccount()
                {
                    Id = mrChatId
                },
                From = new ChannelAccount()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Peter"
                }
            });

            await service.ReturnMessage(context);

            Assert.True(true);
        }
    }
}
