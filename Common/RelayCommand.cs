using System.Diagnostics;
using System.Windows.Input;

namespace JiraClient.Common
{
    public class RelayCommand : ICommand
    {
        private readonly Action mExecute;
        private readonly Func<bool> mCanExecute;

        public RelayCommand(Action executeAction) 
            : this(executeAction, null) 
        {
        }

        public RelayCommand(Action executeAction, Func<bool> canExecuteAction)
        {
            Debug.Assert(executeAction != null, "ExecuteAction is null");

            this.mExecute = executeAction;
            this.mCanExecute = canExecuteAction;
        }

        public event EventHandler CanExecuteChanged
        {
            add { if (mCanExecute != null) CommandManager.RequerySuggested += value; }
            remove { if (mCanExecute != null) CommandManager.RequerySuggested -= value; }
        }

        [DebuggerStepThrough]
        public bool CanExecute(object parameter) { return this.mCanExecute == null ? true : this.mCanExecute(); }
        public void Execute(object parameter) { this.mExecute(); }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> mExecute;
        private readonly Func<T, bool> mCanExecute;

        public RelayCommand(Action<T> executeAction)
            : this(executeAction, null)
        {
        }

        public RelayCommand(Action<T> executeAction, Func<T, bool> canExecuteAction)
        {
            Debug.Assert(executeAction != null, "executeAction is null");

            mExecute = executeAction;
            mCanExecute = canExecuteAction;
        }

        public event EventHandler CanExecuteChanged
        {
            add { if (mCanExecute != null) CommandManager.RequerySuggested += value; }
            remove { if (mCanExecute != null) CommandManager.RequerySuggested -= value; }
        }

        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            return mCanExecute == null ? true : mCanExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            mExecute((T)parameter);
        }
    }

    public class RelayCommand<T1, T2> : ICommand
    {
        private readonly Action<T1, T2> mExecute;
        private readonly Func<T1, T2, bool> mCanExecute;

        public RelayCommand(Action<T1, T2> executeAction)
            : this(executeAction, null)
        {
        }

        public RelayCommand(Action<T1, T2> executeAction, Func<T1, T2, bool> canExecuteAction)
        {
            Debug.Assert(executeAction != null, "executeAction is null");

            mExecute = executeAction;
            mCanExecute = canExecuteAction;
        }

        public event EventHandler CanExecuteChanged
        {
            add { if (mCanExecute != null) CommandManager.RequerySuggested += value; }
            remove { if (mCanExecute != null) CommandManager.RequerySuggested -= value; }
        }

        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            if (mCanExecute == null)
            {
                return true;
            }

            object[] objects = parameter as object[];
            Debug.Assert(objects != null && objects.Length >= 2, "Parameter must be at least two objects", nameof(parameter));

            return mCanExecute((T1)objects[0], (T2)objects[1]);
        }
        public void Execute(object parameter)
        {
            object[] objects = parameter as object[];
            Debug.Assert(objects != null && objects.Length >= 2, "Parameter must be at least two objects", nameof(parameter));

            mExecute((T1)objects[0], (T2)objects[1]);
        }
    }
}
