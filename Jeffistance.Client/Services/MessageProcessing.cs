using System;
using System.Reflection;
using Jeffistance.Common.Services.MessageProcessing;
using Jeffistance.Client.Models;
using ModusOperandi.Messaging;

namespace Jeffistance.Client.Services.MessageProcessing
{
    class ClientMessageProcessor : JeffistanceMessageProcessor
    {
        public override void ProcessMessage(Message message)
        {
            if(message == null)
                return;
            base.ProcessMessage(message);
            LogMessage(message);
        }

        public override void LogMessage(Message message)
        {
            var appState = AppState.GetAppState();
            appState.Log(message.Text, (string) message.Sender);
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
    }

}
