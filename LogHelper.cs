using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogHelperLib
{
    /// <summary>
    /// 异步日志记录封装类
    /// </summary>
    public class LogHelper
    {
        #region 日志记录类型File为txt文件，Database为数据库记录
        /// <summary>
        /// 日志记录类型File为txt文件，Database为数据库记录
        /// </summary>
        public enum LogTarget
        {
            File,
            Database
        }
        #endregion

        #region 实例LogBase
        /// <summary>
        /// 声明
        /// </summary>
        private LogBase logger = null;

        /// <summary>
        /// 根据选择进行LogBase实例
        /// </summary>
        /// <param name="target">日志保存的类型</param>
        /// <param name="path">日志文件夹默认路径，例如：[软件要根目录\\Log]</param>
        public LogHelper(LogTarget target,string path)
        {
            switch (target)
            {
                case LogTarget.File:
                    logger = new FileLogger(path);
                    break;
                case LogTarget.Database:
                    logger = new DBLogger(path);
                    break;
            }

            
        }

        /// <summary>
        /// 根据选择进行LogBase实例
        /// </summary>
        /// <param name="target">日志保存的类型</param>
        public LogHelper(LogTarget target)
        {
            string path = $"{Application.StartupPath}\\Log";
            switch (target)
            {
                case LogTarget.File:
                    logger = new FileLogger(path);
                    break;
                case LogTarget.Database:
                    logger = new DBLogger(path);
                    break;
            }


        }
        #endregion

        #region 写日志
        /// <summary>
        /// 写信息日志
        /// </summary>
        /// <param name="mes"></param>
        public void Info(string mes,string userName="-",int userPermissionsIndex=0)
        {
            logger.Info(mes,userName, userPermissionsIndex);
        }

        /// <summary>
        /// 写错误日志
        /// </summary>
        /// <param name="mes"></param>
        public void Error(string mes, string userName = "-", int userPermissionsIndex = 0)
        {
            logger.Error(mes,userName,userPermissionsIndex);
        }

        /// <summary>
        /// 写报警日志
        /// </summary>
        /// <param name="mes"></param>
        public void Alarm(string mes, string userName = "-", int userPermissionsIndex = 0)
        {
            logger.Alarm(mes,userName,userPermissionsIndex);
        }
        #endregion

        #region 读日志
        /// <summary>
        /// 通过日期查找读取信息日志
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public DataTable ReadInfoByDate(DateTime date)
        {
            return logger.ReadInfoByDate(date);
        }

        /// <summary>
        /// 通过日期区间查找读取信息日志
        /// </summary>
        /// <param name="date1"></param>
        /// <param name="date2"></param>
        /// <returns></returns>
        public DataTable ReadInfoByDateInterval(DateTime date1, DateTime date2)
        {
            return logger.ReadInfoByDateInterval(date1, date2);
        }

        /// <summary>
        /// 通过日期查找读取错误日志
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public DataTable ReadErrorByDate(DateTime date)
        {
            return logger.ReadErrorByDate(date);
        }

        /// <summary>
        /// 通过日期区间查找读取错误日志
        /// </summary>
        /// <param name="date1"></param>
        /// <param name="date2"></param>
        /// <returns></returns>
        public DataTable ReadErrorByDateInterval(DateTime date1, DateTime date2)
        {
            return logger.ReadErrorByDateInterval(date1, date2);
        }

        /// <summary>
        /// 通过日期查找读取报警日志
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public DataTable ReadAlarmByDate(DateTime date)
        {
            return logger.ReadAlarmByDate(date);
        }

        /// <summary>
        /// 通过日期区间查找报警日志
        /// </summary>
        /// <param name="date1"></param>
        /// <param name="date2"></param>
        /// <returns></returns>
        public DataTable ReadAlarmByDateInterval(DateTime date1, DateTime date2)
        {
            return logger.ReadAlarmByDateInterval(date1, date2);
        }

        #endregion

        #region 以文本文件形式记录日志类
        /// <summary>
        /// 以文本文件形式记录日志类
        /// </summary>
        public class FileLogger : LogBase
        {
            public FileLogger(string path) : base(path) { }

            #region 以文本文件形式写日志
            /// <summary>
            /// 以文本文件形式写日志
            /// </summary>
            public override void Write()
            {
                try
                {
                    //根据当天日期创建日志文件
                    var fileName = $"{DateTime.Now.ToString("yyyy-MM-dd")}.log";
                    var infoPath = InfoPath + fileName;
                    var errorPath = ErrorPath + fileName;
                    var alarmPaht = AlarmPath + fileName;

                    //进入写锁
                    _lock.EnterWriteLock();
                    //判断目录是否存在，不存在则重新创建
                    if (!Directory.Exists(InfoPath)) Directory.CreateDirectory(InfoPath);
                    if (!Directory.Exists(ErrorPath)) Directory.CreateDirectory(ErrorPath);
                    //创建StreamWriter
                    StreamWriter swInfo = null;
                    StreamWriter swError = null;
                    StreamWriter swAlarm = null;
                    if (_que?.ToList().Exists(o => o.Level == LogLevel.Info) == true)
                    {
                        swInfo = new StreamWriter(infoPath, true, Encoding.UTF8);
                    }
                    if (_que?.ToList().Exists(o => o.Level == LogLevel.Error) == true)
                    {
                        swError = new StreamWriter(errorPath, true, Encoding.UTF8);
                    }
                    if (_que?.ToList().Exists(o => o.Level == LogLevel.Alarm) == true)
                    {
                        swAlarm = new StreamWriter(alarmPaht, true, Encoding.UTF8);
                    }
                    //判断日志队列中是否有内容，从列队中获取内容，并删除列队中的内容
                    while (_que?.Count > 0 && _que.TryDequeue(out LogMessage logMessage))
                    {
                        var sf = logMessage.StackFrame;
                        switch (logMessage.Level)
                        {
                            case LogLevel.Info:
                                if (swInfo != null)
                                {
                                    swInfo.WriteLine($"[级别]:Info");
                                    swInfo.WriteLine($"[发生时间]:{logMessage.OccurrenceTime.ToString("yyyy-MM-dd HH:mm:ss.ffff")}");
                                    swInfo.WriteLine($"[记录时间]:{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff")}");
                                    swInfo.WriteLine($"[用户]:{logMessage.UserName}");
                                    swInfo.WriteLine($"[用户权限]:{logMessage.UserPermission}");
                                    swInfo.WriteLine($"[类名]:{sf?.GetMethod().DeclaringType.FullName}");
                                    swInfo.WriteLine($"[方法]:{sf?.GetMethod().Name}");
                                    swInfo.WriteLine($"[行号]:{sf?.GetFileLineNumber()}");
                                    swInfo.WriteLine($"[内容]:{logMessage.Message}");
                                    swInfo.WriteLine("------------------------------------------------------------------------------------------");
                                }
                                break;
                            case LogLevel.Error:
                                if (swError != null)
                                {
                                    swError.WriteLine($"[级别]:Error");
                                    swError.WriteLine($"[发生时间]:{logMessage.OccurrenceTime.ToString("yyyy-MM-dd HH:mm:ss.ffff")}");
                                    swError.WriteLine($"[记录时间]:{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff")}");
                                    swError.WriteLine($"[用户]:{logMessage.UserName}");
                                    swError.WriteLine($"[用户权限]:{logMessage.UserPermission}");
                                    swError.WriteLine($"[类名]:{sf?.GetMethod().DeclaringType.FullName}");
                                    swError.WriteLine($"[方法]:{sf?.GetMethod().Name}");
                                    swError.WriteLine($"[行号]:{sf?.GetFileLineNumber()}");
                                    swError.WriteLine($"[内容]:{logMessage.Message}");
                                    swError.WriteLine("------------------------------------------------------------------------------------------");
                                }
                                break;
                            case LogLevel.Alarm:
                                if (swAlarm != null)
                                {
                                    swAlarm.WriteLine($"[级别]:Alarm");
                                    swAlarm.WriteLine($"[发生时间]:{logMessage.OccurrenceTime.ToString("yyyy-MM-dd HH:mm:ss.ffff")}");
                                    swAlarm.WriteLine($"[记录时间]:{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff")}");
                                    swAlarm.WriteLine($"[用户]:{logMessage.UserName}");
                                    swAlarm.WriteLine($"[用户权限]:{logMessage.UserPermission}");
                                    swAlarm.WriteLine($"[类名]:{sf?.GetMethod().DeclaringType.FullName}");
                                    swAlarm.WriteLine($"[方法]:{sf?.GetMethod().Name}");
                                    swAlarm.WriteLine($"[行号]:{sf?.GetFileLineNumber()}");
                                    swAlarm.WriteLine($"[内容]:{logMessage.Message}");
                                    swAlarm.WriteLine("------------------------------------------------------------------------------------------");
                                }
                                break;
                        }
                    }
                    //释放并关闭资源
                    if (swInfo != null)
                    {
                        swInfo.Close();
                        swInfo.Dispose();
                    }
                    if (swError != null)
                    {
                        swError.Close();
                        swError.Dispose();
                    }
                    if (swAlarm != null)
                    {
                        swAlarm.Close();
                        swAlarm.Dispose();
                    }
                }
                finally
                {
                    //退出写锁
                    _lock.ExitWriteLock();
                }
            }
            #endregion

            #region 读取信息日志
            /// <summary>
            /// 通过日期读取信息日志
            /// </summary>
            /// <param name="date"></param>
            /// <returns></returns>
            public override DataTable ReadInfoByDate(DateTime date)
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("级别");
                dt.Columns.Add("发生时间");
                dt.Columns.Add("记录时间");
                dt.Columns.Add("用户");
                dt.Columns.Add("用户权限");
                dt.Columns.Add("类名");
                dt.Columns.Add("方法");
                dt.Columns.Add("行号");
                dt.Columns.Add("内容");
                DirectoryInfo directoryInfo = new DirectoryInfo(InfoPath);
                foreach (FileInfo item in directoryInfo.GetFiles("*.log"))
                {
                    DateTime dateTime = Convert.ToDateTime(item.Name.Replace(".log", ""));
                    if (date.Date == dateTime.Date)
                    {
                        string tempPath = $"{InfoPath}{item.Name}";
                        using (StreamReader reader = new StreamReader(tempPath, Encoding.UTF8))
                        {
                            List<string> content = new List<string>();
                            while (!reader.EndOfStream)
                            {
                                content.Add(reader.ReadLine());
                            }

                            int tempCount1 = dt.Columns.Count;
                            int tempCount2 = content.Count / (tempCount1 + 1);
                            for (int i = 0; i < tempCount2; i++)
                            {
                                DataRow dr = dt.NewRow();
                                for (int j = 0; j < tempCount1; j++)
                                {
                                    dr[j] = content[i * (tempCount1 + 1) + j].Split(new string[] { "]:" }, StringSplitOptions.RemoveEmptyEntries)[1];
                                }
                                dt.Rows.Add(dr);
                            }

                        }
                    }
                }

                //按发生时间降序
                DataView dv = dt.DefaultView;
                dv.Sort = "发生时间 desc";

                return dt;
            }

            /// <summary>
            /// 通过日期区间读取信息日志
            /// </summary>
            /// <param name="date"></param>
            /// <returns></returns>
            public override DataTable ReadInfoByDateInterval(DateTime date1, DateTime date2)
            {
                DateTime minDate = date1 > date2 ? date2 : date1;
                DateTime maxDate = date1 > date2 ? date1 : date2;
                DataTable dt = new DataTable();
                dt.Columns.Add("级别");
                dt.Columns.Add("发生时间");
                dt.Columns.Add("记录时间");
                dt.Columns.Add("用户");
                dt.Columns.Add("用户权限");
                dt.Columns.Add("类名");
                dt.Columns.Add("方法");
                dt.Columns.Add("行号");
                dt.Columns.Add("内容");
                DirectoryInfo directoryInfo = new DirectoryInfo(InfoPath);
                foreach (FileInfo item in directoryInfo.GetFiles("*.log"))
                {
                    DateTime dateTime = Convert.ToDateTime(item.Name.Replace(".log", ""));
                    if (minDate.Date <= dateTime.Date && maxDate.Date >= dateTime.Date)
                    {
                        string tempPath = $"{InfoPath}{item.Name}";
                        using (StreamReader reader = new StreamReader(tempPath, Encoding.UTF8))
                        {
                            List<string> content = new List<string>();
                            while (!reader.EndOfStream)
                            {
                                content.Add(reader.ReadLine());
                            }

                            int tempCount1 = dt.Columns.Count;
                            int tempCount2 = content.Count / (tempCount1 + 1);
                            for (int i = 0; i < tempCount2; i++)
                            {
                                DataRow dr = dt.NewRow();
                                for (int j = 0; j < tempCount1; j++)
                                {
                                    dr[j] = content[i * (tempCount1 + 1) + j].Split(new string[] { "]:" }, StringSplitOptions.RemoveEmptyEntries)[1];
                                }
                                dt.Rows.Add(dr);
                            }

                        }
                    }
                }

                //按发生时间降序
                DataView dv = dt.DefaultView;
                dv.Sort = "发生时间 desc";

                return dt;
            }
            #endregion

            #region 读取错误日志
            /// <summary>
            /// 通过日期读取错误日志
            /// </summary>
            /// <param name="date"></param>
            /// <returns></returns>
            public override DataTable ReadErrorByDate(DateTime date)
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("级别");
                dt.Columns.Add("发生时间");
                dt.Columns.Add("记录时间");
                dt.Columns.Add("用户");
                dt.Columns.Add("用户权限");
                dt.Columns.Add("类名");
                dt.Columns.Add("方法");
                dt.Columns.Add("行号");
                dt.Columns.Add("内容");
                DirectoryInfo directoryInfo = new DirectoryInfo(ErrorPath);
                foreach (FileInfo item in directoryInfo.GetFiles("*.log"))
                {
                    DateTime dateTime = Convert.ToDateTime(item.Name.Replace(".log", ""));
                    if (date.Date == dateTime.Date)
                    {
                        string tempPath = $"{ErrorPath}{item.Name}";
                        using (StreamReader reader = new StreamReader(tempPath, Encoding.UTF8))
                        {
                            List<string> content = new List<string>();
                            while (!reader.EndOfStream)
                            {
                                content.Add(reader.ReadLine());
                            }

                            int tempCount1 = dt.Columns.Count;
                            int tempCount2 = content.Count / (tempCount1 + 1);
                            for (int i = 0; i < tempCount2; i++)
                            {
                                DataRow dr = dt.NewRow();
                                for (int j = 0; j < tempCount1; j++)
                                {
                                    dr[j] = content[i * (tempCount1 + 1) + j].Split(new string[] { "]:" }, StringSplitOptions.RemoveEmptyEntries)[1];
                                }
                                dt.Rows.Add(dr);
                            }

                        }
                    }
                }

                //按发生时间降序
                DataView dv = dt.DefaultView;
                dv.Sort = "发生时间 desc";

                return dt;
            }

            /// <summary>
            /// 通过日期区间读取错误日志
            /// </summary>
            /// <param name="date"></param>
            /// <returns></returns>
            public override DataTable ReadErrorByDateInterval(DateTime date1, DateTime date2)
            {
                DateTime minDate = date1 > date2 ? date2 : date1;
                DateTime maxDate = date1 > date2 ? date1 : date2;
                DataTable dt = new DataTable();
                dt.Columns.Add("级别");
                dt.Columns.Add("发生时间");
                dt.Columns.Add("记录时间");
                dt.Columns.Add("用户");
                dt.Columns.Add("用户权限");
                dt.Columns.Add("类名");
                dt.Columns.Add("方法");
                dt.Columns.Add("行号");
                dt.Columns.Add("内容");
                DirectoryInfo directoryInfo = new DirectoryInfo(ErrorPath);
                foreach (FileInfo item in directoryInfo.GetFiles("*.log"))
                {
                    DateTime dateTime = Convert.ToDateTime(item.Name.Replace(".log", ""));
                    if (minDate.Date <= dateTime.Date && maxDate.Date >= dateTime.Date)
                    {
                        string tempPath = $"{ErrorPath}{item.Name}";
                        using (StreamReader reader = new StreamReader(tempPath, Encoding.UTF8))
                        {
                            List<string> content = new List<string>();
                            while (!reader.EndOfStream)
                            {
                                content.Add(reader.ReadLine());
                            }

                            int tempCount1 = dt.Columns.Count;
                            int tempCount2 = content.Count / (tempCount1 + 1);
                            for (int i = 0; i < tempCount2; i++)
                            {
                                DataRow dr = dt.NewRow();
                                for (int j = 0; j < tempCount1; j++)
                                {
                                    dr[j] = content[i * (tempCount1 + 1) + j].Split(new string[] { "]:" }, StringSplitOptions.RemoveEmptyEntries)[1];
                                }
                                dt.Rows.Add(dr);
                            }

                        }
                    }
                }

                //按发生时间降序
                DataView dv = dt.DefaultView;
                dv.Sort = "发生时间 desc";

                return dt;
            }
            #endregion

            #region 读取报警日志
            /// <summary>
            /// 通过日期读取报警日志
            /// </summary>
            /// <param name="date"></param>
            /// <returns></returns>
            public override DataTable ReadAlarmByDate(DateTime date)
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("级别");
                dt.Columns.Add("发生时间");
                dt.Columns.Add("记录时间");
                dt.Columns.Add("用户");
                dt.Columns.Add("用户权限");
                dt.Columns.Add("类名");
                dt.Columns.Add("方法");
                dt.Columns.Add("行号");
                dt.Columns.Add("内容");
                DirectoryInfo directoryInfo = new DirectoryInfo(AlarmPath);
                foreach (FileInfo item in directoryInfo.GetFiles("*.log"))
                {
                    DateTime dateTime = Convert.ToDateTime(item.Name.Replace(".log", ""));
                    if (date.Date == dateTime.Date)
                    {
                        string tempPath = $"{AlarmPath}{item.Name}";
                        using (StreamReader reader = new StreamReader(tempPath, Encoding.UTF8))
                        {
                            List<string> content = new List<string>();
                            while (!reader.EndOfStream)
                            {
                                content.Add(reader.ReadLine());
                            }

                            int tempCount1 = dt.Columns.Count;
                            int tempCount2 = content.Count / (tempCount1 + 1);
                            for (int i = 0; i < tempCount2; i++)
                            {
                                DataRow dr = dt.NewRow();
                                for (int j = 0; j < tempCount1; j++)
                                {
                                    dr[j] = content[i * (tempCount1 + 1) + j].Split(new string[] { "]:" }, StringSplitOptions.RemoveEmptyEntries)[1];
                                }
                                dt.Rows.Add(dr);
                            }

                        }
                    }
                }

                //按发生时间降序
                DataView dv = dt.DefaultView;
                dv.Sort = "发生时间 desc";

                return dt;
            }

            /// <summary>
            /// 通过日期区间读取报警日志
            /// </summary>
            /// <param name="date"></param>
            /// <returns></returns>
            public override DataTable ReadAlarmByDateInterval(DateTime date1, DateTime date2)
            {
                DateTime minDate = date1 > date2 ? date2 : date1;
                DateTime maxDate = date1 > date2 ? date1 : date2;
                DataTable dt = new DataTable();
                dt.Columns.Add("级别");
                dt.Columns.Add("发生时间");
                dt.Columns.Add("记录时间");
                dt.Columns.Add("用户");
                dt.Columns.Add("用户权限");
                dt.Columns.Add("类名");
                dt.Columns.Add("方法");
                dt.Columns.Add("行号");
                dt.Columns.Add("内容");
                DirectoryInfo directoryInfo = new DirectoryInfo(AlarmPath);
                foreach (FileInfo item in directoryInfo.GetFiles("*.log"))
                {
                    DateTime dateTime = Convert.ToDateTime(item.Name.Replace(".log", ""));
                    if (minDate.Date <= dateTime.Date && maxDate.Date >= dateTime.Date)
                    {
                        string tempPath = $"{AlarmPath}{item.Name}";
                        using (StreamReader reader = new StreamReader(tempPath, Encoding.UTF8))
                        {
                            List<string> content = new List<string>();
                            while (!reader.EndOfStream)
                            {
                                content.Add(reader.ReadLine());
                            }

                            int tempCount1 = dt.Columns.Count;
                            int tempCount2 = content.Count / (tempCount1 + 1);
                            for (int i = 0; i < tempCount2; i++)
                            {
                                DataRow dr = dt.NewRow();
                                for (int j = 0; j < tempCount1; j++)
                                {
                                    dr[j] = content[i * (tempCount1 + 1) + j].Split(new string[] { "]:" }, StringSplitOptions.RemoveEmptyEntries)[1];
                                }
                                dt.Rows.Add(dr);
                            }

                        }
                    }
                }

                //按发生时间降序
                DataView dv = dt.DefaultView;
                dv.Sort = "发生时间 desc";

                return dt;
            }
            #endregion
        }

        #endregion

        #region 以数据库形式记录日志类
        /// <summary>
        /// 以数据库形式记录日志类
        /// </summary>
        public class DBLogger : LogBase
        {
            public DBLogger(string path) : base(path) { }

            public override void Write()
            {

            }

            public override DataTable ReadAlarmByDateInterval(DateTime date1, DateTime date2)
            {
                return new DataTable();
            }

            public override DataTable ReadAlarmByDate(DateTime date)
            {
                return new DataTable();
            }

            public override DataTable ReadInfoByDate(DateTime date)
            {
                return new DataTable();
            }

            public override DataTable ReadInfoByDateInterval(DateTime date1, DateTime date2)
            {
                return new DataTable();
            }

            public override DataTable ReadErrorByDate(DateTime date)
            {
                return new DataTable();
            }

            public override DataTable ReadErrorByDateInterval(DateTime date1, DateTime date2)
            {
                return new DataTable();
            }
        }
        #endregion

        #region 日志记录基类
        /// <summary>
        /// 日志记录基类
        /// </summary>
        public abstract class LogBase
        {
            #region 属性与字段
            /// <summary>
            /// 信息日志记录文件夹路径
            /// </summary>
            public string InfoPath { get; set; }

            /// <summary>
            /// 错误日志记录文件夹路径
            /// </summary>
            public string ErrorPath { get; set; }

            /// <summary>
            /// 报警日志记录文件夹路径
            /// </summary>
            public string AlarmPath { get; set; }


            /// <summary>
            /// 线程安全队列
            /// </summary>
            public readonly ConcurrentQueue<LogMessage> _que;

            /// <summary>
            /// 信号
            /// </summary>
            public readonly ManualResetEvent _mre;

            /// <summary>
            /// 日志写锁
            /// </summary>
            public readonly ReaderWriterLockSlim _lock;
            /// <summary>
            /// StackTrace栈空间的级别，0表示当前栈空间，1表示上一级的栈空间，依次类推
            /// </summary>
            private const int tempIndex = 2;

            #endregion

            public LogBase(string path)
            {
                InfoPath = $"{path}\\Info\\";
                ErrorPath = $"{path}\\Error\\";
                AlarmPath = $"{path}\\Alarm\\";
                if (!Directory.Exists(InfoPath))
                {
                    Directory.CreateDirectory(InfoPath);
                }
                if (!Directory.Exists(ErrorPath))
                {
                    Directory.CreateDirectory(ErrorPath);
                }
                if (!Directory.Exists(AlarmPath))
                {
                    Directory.CreateDirectory(AlarmPath);
                }
                _que = new ConcurrentQueue<LogMessage>();
                _mre = new ManualResetEvent(false);
                _lock = new ReaderWriterLockSlim();
                Task.Run(() => Initialize());
            }

            #region 日志初始化
            /// <summary>
            /// 日志初始化
            /// </summary>
            private void Initialize()
            {
                while (true)
                {
                    //等待信号通知
                    _mre.WaitOne();
                    //写入日志
                    Write();
                    //重新设置信号
                    _mre.Reset();
                    Thread.Sleep(1);
                }
            }
            #endregion

            /// <summary>
            /// 日志写的方法
            /// </summary>
            public abstract void Write();

            #region 读日志信息
            /// <summary>
            /// 通过日期查找读取信息日志
            /// </summary>
            /// <param name="date"></param>
            /// <returns></returns>
            public abstract DataTable ReadInfoByDate(DateTime date);

            /// <summary>
            /// 通过日期区间查找读取信息日志
            /// </summary>
            /// <param name="date1"></param>
            /// <param name="date2"></param>
            /// <returns></returns>
            public abstract DataTable ReadInfoByDateInterval(DateTime date1, DateTime date2);

            /// <summary>
            /// 通过日期查找读取错误日志
            /// </summary>
            /// <param name="date"></param>
            /// <returns></returns>
            public abstract DataTable ReadErrorByDate(DateTime date);

            /// <summary>
            /// 通过日期区间查找读取错误日志
            /// </summary>
            /// <param name="date1"></param>
            /// <param name="date2"></param>
            /// <returns></returns>
            public abstract DataTable ReadErrorByDateInterval(DateTime date1, DateTime date2);

            /// <summary>
            /// 通过日期查找读取报警日志
            /// </summary>
            /// <param name="date"></param>
            /// <returns></returns>
            public abstract DataTable ReadAlarmByDate(DateTime date);

            /// <summary>
            /// 通过日期区间查找读取报警日志
            /// </summary>
            /// <param name="date1"></param>
            /// <param name="date2"></param>
            /// <returns></returns>
            public abstract DataTable ReadAlarmByDateInterval(DateTime date1, DateTime date2);
            #endregion

            #region 错误日志
            /// <summary>
            /// 写错误日志
            /// </summary>
            /// <param name="mes">内容</param>
            public void Error(string mes, string userName, int userPermissionIndex)
            {
                var sf = new StackTrace(true).GetFrame(tempIndex);
                LogMessage logMessage = new LogMessage
                {
                    Level = LogLevel.Error,
                    Message = mes,
                    StackFrame = sf,
                    OccurrenceTime = DateTime.Now,
                    UserName = userName,
                    UserPermission = userPermissionIndex
                };
                _que.Enqueue(logMessage);
                _mre.Set();
            }

            #endregion

            #region 信息日志
            /// <summary>
            /// 写信息日志
            /// </summary>
            /// <param name="mes">内容</param>
            public void Info(string mes, string userName, int userPermissionIndex)
            {
                var sf = new StackTrace(true).GetFrame(2);
                LogMessage logMessage = new LogMessage
                {
                    Level = LogLevel.Info,
                    Message = mes,
                    StackFrame = sf,
                    OccurrenceTime = DateTime.Now,
                    UserName = userName,
                    UserPermission = userPermissionIndex
                };
                _que.Enqueue(logMessage);
                _mre.Set();
            }
            #endregion

            #region 报警日志
            /// <summary>
            /// 写错误日志
            /// </summary>
            /// <param name="mes">内容</param>
            public void Alarm(string mes, string userName = "-", int userPermissionIndex = 0)
            {
                var sf = new StackTrace(true).GetFrame(tempIndex);
                LogMessage logMessage = new LogMessage
                {
                    Level = LogLevel.Alarm,
                    Message = mes,
                    StackFrame = sf,
                    OccurrenceTime = DateTime.Now,
                    UserName = userName,
                    UserPermission = userPermissionIndex
                };
                _que.Enqueue(logMessage);
                _mre.Set();
            }

            #endregion

            #region 日志实体
            /// <summary>
            /// 日志级别
            /// </summary>
            public enum LogLevel
            {
                Info,
                Alarm,
                Error
            }

            /// <summary>
            /// 消息实体
            /// </summary>
            public class LogMessage
            {
                /// <summary>
                /// 日志级别
                /// </summary>
                public LogLevel Level { get; set; }

                /// <summary>
                /// 消息内容
                /// </summary>
                public string Message { get; set; }


                /// <summary>
                /// 堆栈帧信息
                /// </summary>
                public StackFrame StackFrame { get; set; }

                /// <summary>
                /// 消息产生时间
                /// </summary>
                public DateTime OccurrenceTime { get; set; }

                /// <summary>
                /// 用户名
                /// </summary>
                public string UserName { get; set; } = "-";

                /// <summary>
                /// 用户身份
                /// </summary>
                public int UserPermission { get; set; } = 0;
            }
            #endregion
        }
        #endregion
    }



}
