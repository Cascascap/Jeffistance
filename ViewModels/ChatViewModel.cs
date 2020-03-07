using System.Collections.Generic;
using System.Collections.ObjectModel;
using Jeffistance.Models;
using System;
using Avalonia.Controls;
using ReactiveUI;

namespace Jeffistance.ViewModels
{
    public class ChatViewModel : ViewModelBase
    {
        public ChatViewModel()
        {
            Items = new ObservableCollection<ChatMessage>();
        }

        public ObservableCollection<ChatMessage> Items { get; }
        string messageContent;
        public string MessageContent {
            get => messageContent;
            set => this.RaiseAndSetIfChanged(ref messageContent, value);
        }

        public void OnSendClicked()
        {
            Console.WriteLine(MessageContent);
            MessageContent = "";
        }

    }
}