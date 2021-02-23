using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

namespace SWDataReceiver
{
    public class MainWindowModel : INotifyPropertyChanged, IDisposable
    {
        private Recorder recorder;

        public event PropertyChangedEventHandler PropertyChanged;

        public string DataCountLabel => "Data Count: " + recorder.RecordCount;
        public StartCommand StartButtonCommand { get; }
        public StopCommand StopButtonCommand { get; }
        public ClearCommand ClearButtonCommand { get; }
        public SaveAsCommand SaveAsButtonCommand { get; }

        public MainWindowModel()
        {
            recorder = new Recorder(13224);
            recorder.OnRecordCountChanged += () => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DataCountLabel)));

            StartButtonCommand = new StartCommand(recorder);
            StopButtonCommand = new StopCommand(recorder);
            ClearButtonCommand = new ClearCommand(recorder);
            SaveAsButtonCommand = new SaveAsCommand(recorder);
        }

        ~MainWindowModel()
        {
            Dispose();
        }

        public void Dispose()
        {
            recorder?.Dispose();
            recorder = null;
        }

        public class StartCommand : ICommand
        {
            private Recorder recorder;

            public event EventHandler CanExecuteChanged;

            public StartCommand(Recorder recorder)
            {
                this.recorder = recorder;
                recorder.OnRunningStateChanged += () => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }

            public bool CanExecute(object parameter)
            {
                return !recorder.IsRecording;
            }

            public void Execute(object parameter)
            {
                recorder.StartRecord();
            }
        }

        public class StopCommand : ICommand
        {
            private Recorder recorder;

            public event EventHandler CanExecuteChanged;

            public StopCommand(Recorder recorder)
            {
                this.recorder = recorder;
                recorder.OnRunningStateChanged += () => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }

            public bool CanExecute(object parameter)
            {
                return recorder.IsRecording;
            }

            public void Execute(object parameter)
            {
                recorder.StopRecord();
            }
        }

        public class ClearCommand : ICommand
        {
            private Recorder recorder;

            public event EventHandler CanExecuteChanged;

            public ClearCommand(Recorder recorder)
            {
                this.recorder = recorder;
                recorder.OnRunningStateChanged += () => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }

            public bool CanExecute(object parameter)
            {
                return !recorder.IsRecording;
            }

            public void Execute(object parameter)
            {
                recorder.ClearRecords();
            }
        }

        public class SaveAsCommand : ICommand
        {
            private Recorder recorder;

            public event EventHandler CanExecuteChanged;

            public SaveAsCommand(Recorder recorder)
            {
                this.recorder = recorder;
                recorder.OnRunningStateChanged += () => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }

            public bool CanExecute(object parameter)
            {
                return !recorder.IsRecording;
            }

            public void Execute(object parameter)
            {
                var dialog = new SaveFileDialog();
                dialog.Filter = "Csv Files | *.csv";
                dialog.DefaultExt = "csv";

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                using (var file = File.CreateText(dialog.FileName))
                {
                    recorder.SaveRecords(file);
                }
            }
        }
    }
}
