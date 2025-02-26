using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;

namespace JiraClient.Utilities
{
    enum MessageType
    {
        Info = 0x01,
        Warning = 0x02,
        Error = 0x04,
    }

    class LogMessage
    {
        public DateTime Time { get; }
        public MessageType MessageType { get; }
        public string Message { get; }
        public string File { get; }
        public string Caller { get; }
        public int Line { get; }
        public string MetaData => $"{File}: {Caller} ({Line})";

        public LogMessage(MessageType type, string msg, string file, string caller, int line)
        {
            Time = DateTime.Now;
            MessageType = type;
            Message = msg;
            File = Path.GetFileName(file);
            Caller = caller;
            Line = line;
        }
    }

    static class Logger
    {
        private static int mMessageFilter;
        private static readonly ObservableCollection<LogMessage> mMessages;
        public static ReadOnlyObservableCollection<LogMessage> Messages { get; }
        public static CollectionViewSource FilteredMessages { get; }

        static Logger()
        {
            mMessageFilter = (int)(MessageType.Info | MessageType.Warning | MessageType.Error);
            mMessages = new ObservableCollection<LogMessage>();
            Messages = new ReadOnlyObservableCollection<LogMessage>(mMessages);
            FilteredMessages = new CollectionViewSource { Source = Messages };

            FilteredMessages.Filter += (s, e) =>
            {
                LogMessage logMessage = e.Item as LogMessage;
                Debug.Assert(logMessage != null, "log message is null");

                int type = (int)logMessage.MessageType;
                e.Accepted = (type & mMessageFilter) != 0;
            };
        }

        public static async Task Log(MessageType type, string message,
            [CallerFilePath] string file = "",
            [CallerMemberName] string caller = "",
            [CallerLineNumber] int line = 0)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                mMessages.Add(new LogMessage(type, message, file, caller, line));
            });
        }

        public static async Task Clear()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                mMessages.Clear();
            });
        }

        public static void SetMessageFilter(int mask)
        {
            mMessageFilter = mask;
            FilteredMessages.View.Refresh();
        }
    }
}
