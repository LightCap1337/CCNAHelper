﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Utilities;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace CCNAHelper
{
    public partial class Main : Form
    {
        private static HttpListener _listener;

        private bool running = true;

        private string question = "";

        private int answerIndex = 0;

        List<string> answers = new List<string>();

        private Thread checker;

        public Main()
        {
            InitializeComponent();
            Initialize();
            InitializeHooks();
        }

        void Initialize()
        {
           
            BackColor = Color.Lime;
            TransparencyKey = Color.Lime;
            TopMost = true;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;

            Rectangle resolution = new Rectangle(0, 0, 1920, 1080);

            FormClosing += (object sender, FormClosingEventArgs e) => { _listener.Close(); running = false; };

            Location = new Point((int)(resolution.Width * Settings.Instance.Prefs.anchorA.X), (int)(resolution.Height * Settings.Instance.Prefs.anchorA.Y));
            Size = new Size((int)(resolution.Width * Settings.Instance.Prefs.anchorB.X), (int)(resolution.Height * Settings.Instance.Prefs.anchorB.Y));

            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:60024/");
            _listener.Start();
            _listener.BeginGetContext(new AsyncCallback(ProcessRequest), null);
        }

        void ProcessRequest(IAsyncResult result)
        {
            if (!running) return;
            HttpListenerContext context = _listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;

            string postData;
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                postData = reader.ReadToEnd();
                if (postData.Split("=".ToArray()[0])[1].Replace("+", " ") != question)
                {
                    question = postData.Split("=".ToArray()[0])[1].Replace("+", " ");
                    FindAnswer(question);
                }
            }
            

            _listener.Close();
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:60024/");
            _listener.Start();
            _listener.BeginGetContext(new AsyncCallback(ProcessRequest), null);
        }

        void StartListener()
        {

        }

        void InitializeHooks()
        {
            globalKeyboardHook gkh = new globalKeyboardHook();
            gkh.HookedKeys.Add(Keys.Escape);
            gkh.HookedKeys.Add(Keys.E);
            gkh.HookedKeys.Add(Keys.Q);
            gkh.HookedKeys.Add(Settings.Instance.Prefs.toggleShowKey);
            gkh.HookedKeys.Add(Settings.Instance.Prefs.showKey);
            gkh.HookedKeys.Add(Settings.Instance.Prefs.hideKey);
            gkh.KeyDown += new KeyEventHandler(gkh_KeyDown);
            gkh.KeyUp += new KeyEventHandler(gkh_KeyUp);
        }

        void gkh_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                running = false;
                Environment.Exit(0);
            }
            if (e.KeyCode == Keys.Q)
            {
                answerIndex--;
                if (answerIndex < 0) answerIndex = 0;
                ShowAnswer();
            }
            if (e.KeyCode == Keys.E)
            {
                answerIndex++;
                answerIndex %= answers.Count;
                ShowAnswer();
            }
            if (Settings.Instance.Prefs.showMode)
            {
                if (e.KeyCode == Settings.Instance.Prefs.toggleShowKey)
                {
                    Show();
                }
            }
            else
            {
                if (e.KeyCode == Settings.Instance.Prefs.showKey)
                {
                    Show();
                }
                if (e.KeyCode == Settings.Instance.Prefs.hideKey)
                {
                    Hide();
                }
            }
        }

        void gkh_KeyUp(object sender, KeyEventArgs e)
        {
            if (Settings.Instance.Prefs.showMode)
            {
                if (e.KeyCode == Settings.Instance.Prefs.toggleShowKey)
                {
                    Hide();
                }
            }
        }

        void FindAnswer(string question)
        {
            SetLabelText(label1,"");

            answers = new List<string>();
           foreach (Question q in Settings.Instance.Questions)
           {
                        
               if (q.Body.Contains(question))
               {
                   foreach (string s in q.Answers)
                   {
                        answers.Add(s);
                   }
               }
                     
           }

            answerIndex = 0;
            ShowAnswer();
                /*
                if (Settings.Instance.Prefs.onlineMode)
                {
                    SetLabelText(label2, "Serching for answer on server ...");
                    Thread t = new Thread(new ThreadStart(() => FindOnlineAnswer(question)));
                    t.Start();
                };
                */
        }

        void ShowAnswer()
        {
            if (answers.Count == 0)
            {
                SetLabelText(label1, "No answers found");
            }
            else
            {
                SetLabelText(label1, answers[answerIndex]);
            }
        }

        void FindOnlineAnswer(string question)
        {
            string html = string.Empty;
            string url = @"http://localhost/api/Answer.php?question=" + question + "&key=" + Settings.Instance.Prefs.apiKey;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            string[] answers = new string[0];

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    html = reader.ReadToEnd();
                    if (!html.Contains("Error"))
                    {
                        answers = JsonConvert.DeserializeObject<string[]>(html);
                    }
                    else
                    {
                        SetLabelText(label2, html);
                        return;
                    }

                }
                SetLabelText(label2, "");
                foreach (string a in answers)
                {
                    AddLabelText(label2, a + "\n");
                }
            }
            catch (Exception e)
            {
                SetLabelText(label2, "Cant connect to Server (" + e.Message + ")");
            }
        }

        void SetLabelText(Label l, string s)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => { SetLabelText(l, s); }));
            }
            else
            {
                l.Text = s;
            }
        }

        void AddLabelText(Label l, string s)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => { AddLabelText(l, s); }));
            }
            else
            {
                l.Text += s;
            }
        }

        string GetText(Label l)
        {
           return l.Text;
        }
    }
}
