using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SWDataReceiver
{
    public class Recorder : IDisposable
    {
        private class Record
        {
            public float[] Numbers;
            public uint Bools;
        }

        private static readonly byte[] okResponse = Encoding.UTF8.GetBytes("OK");

        private SynchronizedCollection<Record> records = new SynchronizedCollection<Record>();
        private int maxChannelCount;

        private HttpListener listener;
        private CancellationTokenSource cts;
        private Task task;

        public bool IsRecording { get; private set; }
        public int RecordCount => records.Count;

        public event Action OnRunningStateChanged;
        public event Action OnRecordCountChanged;

        public Recorder(int port)
        {
            listener = new HttpListener();
            listener.Prefixes.Add($"http://+:{port}/commit/");

            cts = new CancellationTokenSource();
            task = Task.Run(() => {
                listener.Start();
                while (true)
                {
                    HttpListenerContext context;
                    try {
                        context = listener.GetContext();
                    }
                    catch
                    {
                        return;
                    }
                    
                    var data = context.Request.QueryString.Get("data");
                    context.Response.OutputStream.Write(okResponse, 0, okResponse.Length);
                    context.Response.Close();

                    OnReceive(data);
                }
            }, cts.Token);
        }
        
        ~Recorder()
        {
            Dispose();
        }

        public void Dispose()
        {
            cts?.Cancel();
            cts = null;
            listener?.Abort();
            listener = null;
            task?.Wait();
            task = null;
        }

        private void OnReceive(string data)
        {
            if (!IsRecording)
            {
                return;
            }
            
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            data = data.Replace('-', '+').Replace('_', '/');
            var bytes = Convert.FromBase64String(data);
            if (bytes.Length != 2+33 * 4)
            {
                return;
            }

            var channelCount = bytes[1];
            var bools = BitConverter.ToUInt32(bytes, 2);
            var numbers = new float[32];
            for (var i = 0; i < 32; ++i)
            {
                numbers[i] = BitConverter.ToSingle(bytes, 6 + i * 4);
            }

            lock(records.SyncRoot)
            {
                records.Add(new Record
                {
                    Numbers = numbers,
                    Bools = bools,
                });
                maxChannelCount = Math.Max(maxChannelCount, channelCount);
            }

            OnRecordCountChanged?.Invoke();
        }

        public void StartRecord()
        {
            IsRecording = true;
            OnRunningStateChanged?.Invoke();
        }

        public void StopRecord()
        {
            IsRecording = false;
            OnRunningStateChanged?.Invoke();
        }

        public void ClearRecords()
        {
            if (IsRecording)
            {
                return;
            }

            records.Clear();
            maxChannelCount = 0;
            OnRecordCountChanged?.Invoke();
        }

        public void SaveRecords(StreamWriter sw)
        {
            if (IsRecording)
            {
                return;
            }

            lock (records.SyncRoot)
            {
                if (records.Count == 0)
                {
                    return;
                }

                var temporaryList = new List<string>();

                for (var i = 0; i < maxChannelCount; ++i)
                {
                    temporaryList.Add("Number " + (i + 1));
                }
                for (var i = 0; i < maxChannelCount; ++i)
                {
                    temporaryList.Add("Bool " + (i + 1));
                }

                sw.WriteLine(string.Join(",", temporaryList));

                foreach (var record in records)
                {
                    temporaryList.Clear();
                    
                    for (var i = 0; i < maxChannelCount; ++i)
                    {
                        temporaryList.Add(record.Numbers[i].ToString());
                    }
                    
                    for (var i = 0; i < maxChannelCount; ++i)
                    {
                        temporaryList.Add((record.Bools & (1 << i)) != 0 ? "1" : "0");
                    }

                    sw.WriteLine(string.Join(",", temporaryList));
                }
            }
        }
    }
}
