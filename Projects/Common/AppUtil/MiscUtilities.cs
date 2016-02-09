using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO;

namespace Naboo.AppUtil
{
    public class AsyncCallHelper
    {
        private DispatcherTimer _timer;

        public AsyncCallHelper(Action callback, int dueTimeInMilliSeconds)
        {
            _timer = new DispatcherTimer();

            _timer.Interval = new TimeSpan(0, 0, 0, 0, dueTimeInMilliSeconds);
            _timer.Tick +=
                (s, e) =>
                {
                    _timer.Stop();
                    callback();
                }
                ;

            _timer.Start();
        }

        public static void DelayCall(Action callback, int dueTimeInMilliSeconds = 1)
        {
            AsyncCallHelper asyncCall = new AsyncCallHelper(callback, dueTimeInMilliSeconds);
        }
    }

#if NODO
    public static class NoDoHelper
    {
        public static void CopyTo(this Stream srcStream, Stream destStream)
        {

            byte[] tempBuf = new byte[4096];
            int bytesToRead = (int)srcStream.Length;
            int bytesRead = 0;

            while (bytesToRead > 0)
            {
                int n = srcStream.Read(tempBuf, 0, 4096);

                if (n <= 0)
                {
                    break;
                }

                destStream.Write(tempBuf, 0, n);

                bytesRead += n;
                bytesToRead -= n;
            }
        }

        public static void MoveFile(this System.IO.IsolatedStorage.IsolatedStorageFile isoStorage, string srcFile, string destFile)
        {
            using (Stream srcStream = isoStorage.OpenFile(srcFile, FileMode.Open))
            {
                using (Stream destStream = isoStorage.OpenFile(destFile, FileMode.Create))
                {
                    srcStream.CopyTo(destStream);
                }
            }
        }
    }
#endif

}
