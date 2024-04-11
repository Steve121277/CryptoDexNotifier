
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using System.Net.Http;
using System.Runtime.Remoting.Contexts;
using System.Threading.Tasks;

namespace CryptoDexNotifier
{
    class CryptoDexNotifierThread
    {
        DataManager dataManager;
        public bool bStop = false;
        private Thread threadSavePrice;
        private Thread threadSendAlert;

        public static bool bRunRightNow = false;
        public bool gProcessing = false;
        public DateTime gLastCheckTime = DateTime.MinValue;

        public static bool bSalesRunRightNow = false;
        public bool gSalesProcessing = false;
        public DateTime gLastSalesCheckTime = DateTime.MinValue;

        public static string LogDir = AppDomain.CurrentDomain.BaseDirectory + "Logs";

        public bool m_bRunning = false;

        public CryptoDexNotifierThread()
        {
            dataManager = new DataManager();
        }

        public void Start()
        {
            Directory.CreateDirectory(LogDir);

            bStop = false;
            threadSavePrice = new Thread(SaveDexTokenPrice);
            threadSavePrice.Start();

            threadSendAlert = new Thread(ProcessSendAlert);

            // threadSalesPerson = new Thread(ProcessSalesPersonNotifier);
            // threadSalesPerson.Start();
        }

        public void Stop()
        {
            bStop = true;
            threadSavePrice.Abort();
            threadSendAlert.Abort();
        }

        
        public void SaveDexTokenPrice()
        {
            while (!bStop)
            {
                DateTime currentTime1 = DateTime.Now;
                DataManager.TokenDexAPI();
                DateTime currentTime2 = DateTime.Now;

                TimeSpan sp = currentTime2 - currentTime1;

                if (1000 > sp.Milliseconds)
                {
                    Thread.Sleep(1000 - sp.Milliseconds);
                }
            }
        }

        public void ProcessSendAlert()
        {
            while(!bStop)
            {
                DataManager.SendAlert();
            }
        }

        public void ProcessSalesPersonNotifier()
        {
            while (!bStop)
            {
                DateTime currentTime = DateTime.Now;
                DateTime tmpTime = gLastSalesCheckTime;
                tmpTime = tmpTime.AddHours(3);

                bool bDoCheck = false;
                if (gLastSalesCheckTime == DateTime.MinValue)
                {
                    gLastSalesCheckTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, 15, 0, 0);
                    gLastSalesCheckTime = gLastSalesCheckTime.AddHours(-3);

                    Log(string.Format("Run Sales Right now {0} ...", bSalesRunRightNow == true ? 1 : 0));

                    if (bSalesRunRightNow)
                        bDoCheck = true;
                    else
                        continue;
                }

                if (!bDoCheck && !gSalesProcessing && tmpTime < currentTime)
                {
                    bDoCheck = true;
                }

                if (bDoCheck)
                {
                    gLastSalesCheckTime = currentTime;
                    gSalesProcessing = true;
                    Log("SalesPerson Notifier Starting ...");
                    dataManager.RunSalesPerson();
                    Log("SalesPerson Notifier Done");
                    gSalesProcessing = false;
                }

                Thread.Sleep(1000* 3600);
            }

        }

        public void Log(string param)
        {
            dataManager.OnLogEvent(LOG_EVENT.ONNOTICEMSG, param);
        }
    }
}
