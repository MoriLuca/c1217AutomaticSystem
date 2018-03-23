using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OmronOpener
{
    public partial class Service1 : ServiceBase
    {
        OMRON.Compolet.CIP.SgwServiceCompolet s = new OMRON.Compolet.CIP.SgwServiceCompolet();
        OMRON.Compolet.CIP.ServiceStatus status;
        OMRON.Compolet.CIP.CIPPortCompolet p = new OMRON.Compolet.CIP.CIPPortCompolet();
        Luca.Logger _log;
        private bool _serviceOK;

        public Service1()
        {
            InitializeComponent();
            this._log = new Luca.Logger(@"\GiDi_Runner\AutoLoaderSerivice");
        }

        protected override void OnStart(string[] args)
        {
            Thread t = new Thread(CheckOmron);
            t.IsBackground = true;
            t.Start();
            _log.WriteLog("*** START OF THE SERVICE. The service was started.");
        }

        protected override void OnStop()
        {
            _log.WriteLog("*** END OF THE SERVICE. The service was stopped.");
        }

        private void CheckOmron()
        {
            while (true)
            {
                Thread.Sleep(5000);
                try
                {
                    status = s.ServiceStatus;

                    //Se in funzione e porta aperta
                    if (status == OMRON.Compolet.CIP.ServiceStatus.Running && p.IsOpened(2))
                    {
                        if (!_serviceOK)
                        {
                            _log.WriteLog("*** Turned ON. Service is ON, ad port is Open.");
                            _serviceOK = true;
                        }
                        continue;
                    }

                    if (status == OMRON.Compolet.CIP.ServiceStatus.Running && !p.IsOpened(2))
                    {
                        if (!p.IsOpened(2)) p.Open(2);
                        _log.WriteLog("CIP Service was Running but Port 2 Was Closed");
                        _serviceOK = false;
                        continue;
                    }


                    if (status == OMRON.Compolet.CIP.ServiceStatus.StartPending ||
                        status == OMRON.Compolet.CIP.ServiceStatus.StopPending ||
                        status == OMRON.Compolet.CIP.ServiceStatus.ContinuePending)
                    {
                        _log.WriteLog("There is a pending Request : " + status.ToString());
                        _serviceOK = false;
                        continue;
                    }

                    else
                    {
                        s.StartService();
                        _log.WriteLog("CIP Service was OFF. Turning ON ...");
                        _serviceOK = false;
                    }

                }
                catch (Exception ex)
                {
                    _log.WriteLog("!!!! Error On The Service : " +ex.Message);
                }

            }
        }
    }
}
