using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace SWDataReceiver
{
    public class MainWindowModel : INotifyPropertyChanged, IDisposable
    {
        private Recorder recorder;
        private DispatcherTimer timer;
        private bool recordCountDirty;

        public event PropertyChangedEventHandler PropertyChanged;

        public string DataCountLabel => "Data Count: " + recorder.RecordCount;
        public StartCommand StartButtonCommand { get; }
        public StopCommand StopButtonCommand { get; }
        public ClearCommand ClearButtonCommand { get; }
        public SaveAsCommand SaveAsButtonCommand { get; }

        public MainWindowModel()
        {
            recorder = new Recorder(13224);
            recorder.OnRecordCountChanged += () => recordCountDirty = true;

            StartButtonCommand = new StartCommand(recorder);
            StopButtonCommand = new StopCommand(recorder);
            ClearButtonCommand = new ClearCommand(recorder);
            SaveAsButtonCommand = new SaveAsCommand(recorder);

            timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!recordCountDirty)
            {
                return;
            }

            recordCountDirty = false;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DataCountLabel)));
        }

        ~MainWindowModel()
        {
            Dispose();
        }

        public void Dispose()
        {
            recorder?.Dispose();
            recorder = null;

            timer?.Stop();
            timer = null;
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

                try
                {
                    using (var file = File.CreateText(dialog.FileName))
                    {
                        recorder.SaveRecords(file);
                    }
                }
                catch(UnauthorizedAccessException e)
                {
                    MessageBox.Show("ファイルが開けませんでした。", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
