using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Configuration;

namespace CryptoDexNotifier
{
    class BaseManager
    {
        const string LogDir = "Logs";
        const string LogFilePrefix = "EmailNotifier_";

        public string m_strLogFile;
        public FileStream m_fileStream = null;
        public delegate void LogEventHandler(LOG_EVENT evt, object param);
        public bool bError = false;

        public void AppendLog(LOG_EVENT evt, string msg)
        {
            OnLogEvent(evt, msg);
        }

        public void OnLogEvent(LOG_EVENT evt, object param)
        {
            DateTime now = DateTime.Now;
            string str2 = string.Format("{0:D4}/{1:D2}/{2:D2} {3:D2}:{4:D2}:{5:D2} ->", new object[] { now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second });
            string str3 = " ";
            switch (evt)
            {
                case LOG_EVENT.ONERRORMSG:
                    str3 = " === ERROR === ";
                    break;

                case LOG_EVENT.ONDEBUGMSG:
                    str3 = " ### DEBUG ### ";
                    break;
            }
            string strMsg = string.Format("{0}{1}{2}\n", str2, str3, param.ToString());
            Console.WriteLine(strMsg);
            this.LogToFile(strMsg);
        }

        public static void SetServiceError(bool bError)
        {
            String query = "UPDATE ErrorService SET Error=@Error WHERE Name=@Name";

            try
            {
                SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["Main"].ToString());
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    if (bError)
                        cmd.Parameters.AddWithValue("@Error", 1);
                    else
                        cmd.Parameters.AddWithValue("@Error", 0);

                    cmd.Parameters.AddWithValue("@Name", "EmailNotifier");
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException e)
                    {
                    }
                }
                con.Close();
            }
            catch (InvalidOperationException ex)
            {
            }

        }

        public void DoLog(string Service, string ErrorPart, string Message)
        {
            if( ErrorPart.Length > 255 ) ErrorPart = ErrorPart.Substring(0, 255);
            if( Message.Length > 450 ) Message = Message.Substring(0, 450);
            this.bError = true;
            String query = "INSERT INTO ErrorLog (ID, Service, ErrorPart, ErrorMsg) values (@ID, @Service, @ErrorPart, @ErrorMsg)";

            try
            {
                SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["Main"].ToString());
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@ID", Guid.NewGuid());
                    cmd.Parameters.AddWithValue("@Service", Service);
                    cmd.Parameters.AddWithValue("@ErrorPart", ErrorPart);
                    cmd.Parameters.AddWithValue("@ErrorMsg", Message);
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException e)
                    {
                    }
                }
                con.Close();
            }
            catch (InvalidOperationException ex)
            {
            }

        }

        private void LogToFile(string strMsg)
        {
            try
            {
                this.m_strLogFile = AppDomain.CurrentDomain.BaseDirectory + LogDir + "\\" + LogFilePrefix + DateTime.Now.ToString("yyyyMMdd") + ".log";

                if (this.m_strLogFile.Length == 0)
                {
                    return;
                }

                lock (this)
                {
                    this.m_fileStream = File.Open(this.m_strLogFile, FileMode.Append, FileAccess.Write, FileShare.Read);

                    StreamWriter writer = new StreamWriter(this.m_fileStream, Encoding.UTF8);
                    writer.Write(strMsg);
                    writer.Close();

                    this.m_fileStream.Close();
                }

                if (strMsg.ToLower().IndexOf("error") >= 0 || strMsg.ToLower().IndexOf("except") >= 0)
                {
                    DoLog("EmailNotifier", this.m_strLogFile, strMsg);
                }
            }
            catch (Exception exception)
            {
                string str = string.Format("Log add failed: {0}", exception.Message);
            }
        }
    }
}
