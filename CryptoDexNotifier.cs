
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace CryptoDexNotifier
{
    public partial class CryptoDexNotifier : ServiceBase
    {
        public CryptoDexNotifier()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            emailnotifierThread = new CryptoDexNotifierThread();
            emailnotifierThread.Start();
        }

        public void onDebug()
        {
            CryptoDexNotifierThread.bRunRightNow = true;
            this.OnStart(null);          
        }

        protected override void OnStop()
        {
            emailnotifierThread.Stop();
        }

        private CryptoDexNotifierThread emailnotifierThread;
    }
}
