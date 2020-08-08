using System;
using System.Reflection;
using Jeffistance.Common.Services.MessageProcessing;
using Jeffistance.Client.Models;
using ModusOperandi.Messaging;
using Jeffistance.Client.ViewModels;
using Avalonia.Threading;
using System.Collections.Generic;
using Jeffistance.Common.Models;
using Jeffistance.Common.Services.IoC;
using Jeffistance.Common.Services;

namespace Jeffistance.Client.Services.MessageProcessing
{
    class ClientMessageProcessor : JeffistanceMessageProcessor
    {
        public override void ProcessMessage(Message message)
        {
            var logger = IoCManager.GetClientLogger();
            LogMessage(logger, message);

            base.ProcessMessage(message);
        }
        
        [MessageMethod(JeffistanceFlags.Update)]
        private void UpdateFlagMethod(Message message)
        {
            var appState = AppState.GetAppState();
            while(message.TryPop(out object result))
            {
                (object obj, string name) = (ValueTuple<object, string>) result;
                PropertyInfo pi = appState.GetType().GetProperty(name);
                pi?.SetValue(appState, obj);
            }

        }

        [MessageMethod(JeffistanceFlags.LobbyReady)]
        private void LobbyReadyFlagMethod(Message message)
        {
            AppState.GetAppState().CurrentLobby.AddReadyUser(Guid.Parse((string) message["UserID"]));
        }

        [MessageMethod(JeffistanceFlags.Chat)]
        private void ChatFlagMessage(Message message)
        {
            AppState appState = AppState.GetAppState();
            
            string username = message.TryPop(out object userId, "UserID")?
            appState.GetUserByID((string) userId).Name : null;
            string MessageID = message.TryPop(out object msgId, "MessageID")?
            (string)msgId: null;
            appState.Log(message.Text, username, MessageID);
        }

        [MessageMethod(JeffistanceFlags.EditChatMessage)]
        private void EditChatMessageFlagMethod(Message message)
        {
            AppState appState = AppState.GetAppState();
            string msgId = (string) message["MessageID"];
            string newText = (string) message["NewText"];
            appState.EditChatMessage(msgId, newText);
        }
        
        [MessageMethod(JeffistanceFlags.DeleteChatMessage)]
        private void DeleteChatMessageFlagMethod(Message message)
        {
            AppState appState = AppState.GetAppState();
            string msgId = (string) message["MessageID"];
            appState.DeleteChatMessage(msgId);
        }

        [MessageMethod(JeffistanceFlags.JoinGameMessage)]
        private void JoinGameMessageFlagMethod(Message message)
        {
            LobbyViewModel lobby = AppState.GetAppState().CurrentLobby;
            Dispatcher.UIThread.Post(() => lobby.MoveToGameScreen());
        }

        [MessageMethod(JeffistanceFlags.EveryoneReadyStateMessage)]
        private void EveryoneReadyStateMessage(Message message)
        {
            var appState = AppState.GetAppState();
            if (appState.CurrentUser.IsHost)
            {
                appState.CurrentLobby.OnEveryoneReadyStateChange((bool) message["readyState"]);
            }
        }

        [MessageMethod(JeffistanceFlags.GameStateUpdateMessage)]
        private void GameStateUpdateMessage(Message message)
        {
            var gameState = (GameState) message["GameState"];
            Dispatcher.UIThread.Post(() =>
            {
                AppState appState = AppState.GetAppState();
                GameScreenViewModel gameScreen = appState.CurrentWindow as GameScreenViewModel;
                gameScreen.OnGameStateUpdate(gameState);
            });
        }
    }
}
