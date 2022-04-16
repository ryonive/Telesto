﻿using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace Telesto
{
    
    internal class Endpoint : IDisposable
    {

        internal class Context
        {

            public HttpListener Listener { get; set; }
            public string Endpoint { get; set; }
            public bool Running { get; set; }
            public Thread CtxThread { get; set; }

        }

        internal enum StatusEnum
        {
            Unchanged,
            Starting,
            Started,
            Stopping,
            Stopped
        }

        internal Plugin plug { get; set; }        
        internal StatusEnum Status { get; set; }
        internal string StatusDescription { get; set; } = "zzz";
        private Context curctx = null;

        public Endpoint()
        {
            SetStatus(StatusEnum.Stopped, null);
        }

        private void SetStatus(StatusEnum st, string desc)
        {
            if (st != StatusEnum.Unchanged)
            {
                Status = st;
            }
            if (desc != null)
            {
                StatusDescription = string.Format("[{0}] {1}", DateTime.Now, desc);
            }
        }

        public void Start()
        {
            try
            {
                SetStatus(StatusEnum.Starting, null);                
                HttpListener http = new HttpListener();
                http.Prefixes.Clear();
                http.Prefixes.Add(plug._cfg.HttpEndpoint);
                Thread th = new Thread(new ParameterizedThreadStart(ThreadProc));
                Context ctx = new Context() { Endpoint = plug._cfg.HttpEndpoint, Running = true, CtxThread = th, Listener = http };
                lock (this)
                {
                    if (curctx != null)
                    {
                        curctx.Listener.Abort();
                        curctx.Running = false;
                    }
                    curctx = ctx;
                }
                http.Start();
                th.Name = "Telesto endpoint";
                th.Start(ctx);
            }
            catch (Exception ex)
            {
                Stop();
                SetStatus(StatusEnum.Unchanged, String.Format("Exception: {0} @ {1}", ex.Message, ex.StackTrace));
            }
        }

        public void Stop()
        {
            SetStatus(StatusEnum.Stopping, null);
            lock (this)
            {
                if (curctx != null)
                {
                    curctx.Listener.Abort();
                    curctx.Running = false;
                }
                curctx = null;
            }
            SetStatus(StatusEnum.Stopped, null);
        }

        public void Dispose()
        {
            Stop();
        }

        public void ThreadProc(object o)
        {
            Context ctx = (Context)o;
            HttpListener http = ctx.Listener;
            SetStatus(StatusEnum.Started, String.Format("Waiting for connections on {0}", ctx.Endpoint));
            while (ctx.Running == true && http.IsListening == true)
            {
                try
                {
                    HttpListenerContext hctx = null;
                    string resp = null;
                    try
                    {
                        hctx = http.GetContext();
                        HttpListenerRequest req = hctx.Request;
                        if (req.HttpMethod != "POST")
                        {
                            continue;
                        }
                        string body;
                        using (StreamReader sr = new StreamReader(req.InputStream, req.ContentEncoding))
                        {
                            body = sr.ReadToEnd();
                        }
                        hctx.Response.StatusCode = 200;
                        resp = plug.ProcessTelegram(body);
                        if (resp != null)
                        {
                            Stream s = hctx.Response.OutputStream;
                            byte[] buf = Encoding.UTF8.GetBytes(resp);
                            s.Write(buf, 0, buf.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (hctx != null)
                        {
                            hctx.Response.StatusCode = 500;
                        }
                        SetStatus(StatusEnum.Unchanged, String.Format("Exception: {0} @ {1}", ex.Message, ex.StackTrace));
                    }
                    if (hctx != null)
                    {
                        hctx.Response.Close();
                    }
                }
                catch (Exception ex)
                {
                    SetStatus(StatusEnum.Unchanged, String.Format("Exception: {0} @ {1}", ex.Message, ex.StackTrace));
                }
            }
        }

    }

}
