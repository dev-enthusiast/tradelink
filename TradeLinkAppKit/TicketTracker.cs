﻿using System;
using System.Collections.Generic;
using TradeLink.API;
using TradeLink.Common;
using System.ComponentModel;

namespace TradeLink.AppKit
{
    /// <summary>
    /// push tickets to assembla on a seperate thread, while program does other things
    /// </summary>
    public sealed class TicketTracker
    {
        string _space = string.Empty;
        string _prog = string.Empty;
        /// <summary>
        /// space used by tracker
        /// </summary>
        public string Space { get { return _space; } }
        /// <summary>
        /// program used by tracker
        /// </summary>
        public string Program { get { return _prog; } set { _prog = value; }  }
        string _un = string.Empty;
        string _pw = string.Empty;
        /// <summary>
        /// create a ticket tracker
        /// </summary>
        /// <param name="space"></param>
        /// <param name="program"></param>
        /// <param name="user"></param>
        /// <param name="pw"></param>
        public TicketTracker(string space, string program,string user, string pw)
        {
            _space = space;
            _prog = program;
            _un = user;
            _pw = pw;
        }
        /// <summary>
        /// start the tracker (optional)
        /// </summary>
        public void Start()
        {
            if (!_go)
            {
                _bw.WorkerSupportsCancellation = true;
                _bw.DoWork += new DoWorkEventHandler(_bw_DoWork);
                _go = true;
                _bw.RunWorkerAsync();
            }
        }
        /// <summary>
        /// stop the tracker
        /// </summary>
        public void Stop()
        {
            try
            {
                debug("Ticket tracker stopping");
                if (PushTracksOnClose)
                    debug("Pushing tracks on close and waiting...");
                int closeattempts = 0;
                while (PushTracksOnClose && _untrackedqueue.hasItems && (((_maxattemps != 0) && (closeattempts++ < _maxattemps)) || (_maxattemps == 0)))
                {

                    _SLEEP = 10;
                    System.Threading.Thread.Sleep(10);
                }
                debug("Canceling background threads");
                _go = false;
                _bw.CancelAsync();
            }
            catch { }
        }

        /// <summary>
        /// submit a ticket to portal in background
        /// </summary>
        /// <param name="ticket"></param>
        public void Track(AssemblaTicket ticket)
        {
            if (!TrackEnabled) return;
            if (!_go) Start();
            _tickets++;
            _untrackedqueue.Write(ticket);
        }

        /// <summary>
        /// submit a ticket to portal in background
        /// </summary>
        /// <param name="summary"></param>
        /// <param name="description"></param>
        /// <param name="pri"></param>
        public void Track(string summary, string description, AssemblaPriority pri)
        {
            Track(new AssemblaTicket(summary, description, pri, AssemblaStatus.New));
        }
        /// <summary>
        /// submit a ticket to portal in background
        /// </summary>
        /// <param name="summary"></param>
        /// <param name="description"></param>
        public void Track(string summary, string description)
        {
            Track(new AssemblaTicket(summary, description,  AssemblaPriority.Normal, AssemblaStatus.New));
        }

        /// <summary>
        /// submit a ticket to portal in background
        /// </summary>
        /// <param name="summary"></param>
        /// <param name="description"></param>
        /// <param name="pri"></param>
        /// <param name="stat"></param>
        public void Track(string summary, string description, AssemblaPriority pri, AssemblaStatus stat)
        {
            Track(new AssemblaTicket(summary, description, pri, stat));
        }

        int _tickets = 0;
        /// <summary>
        /// number of tickets tracked lifetime
        /// </summary>
        public int AcceptedTickets { get { return _tickets; } }

        int _SLEEP = 1500;
        /// <summary>
        /// wait between tracks
        /// </summary>
        public int InterTrackSleep { get { return _SLEEP; } set { _SLEEP = value; } }
        RingBuffer<AssemblaTicket> _untrackedqueue = new RingBuffer<AssemblaTicket>(100);
        void _bw_DoWork(object sender, DoWorkEventArgs e)
        {
            while (_go)
            {
                if (e.Cancel) break;
                System.Threading.Thread.Sleep(_SLEEP);
                if ((Space == string.Empty)|| (_un==string.Empty)) continue;
                while (!_untrackedqueue.isEmpty && TrackEnabled)
                {
                    if (e.Cancel) break;
                    // get item
                    AssemblaTicket t = _untrackedqueue.Read();
                    try
                    {
                        int tid = AssemblaTicket.Create(Space, _un, _pw, t.Summary, t.Description, t.Status, t.Priority);
                        debug("created ticket: " + tid);
                    }
                    catch (Exception ex)
                    {
                        debug("error uploading: " + t.ToString() + " " + ex.Message + ex.StackTrace);
                    }
                    System.Threading.Thread.Sleep((int)((double)_SLEEP / 10));
                }
                

            }
        }

        void debug(string msg)
        {
            if (SendDebug != null)
            {
                try
                {
                    SendDebug(msg);
                }
                catch { }
            }
        }


        bool _postallonclose = true;
        /// <summary>
        /// push any untracked events when form is closed
        /// </summary>
        public bool PushTracksOnClose { get { return _postallonclose; } set { _postallonclose = value; } }

        int _maxattemps = 50;
        /// <summary>
        /// maximum number of attempts to push tracks on close
        /// </summary>
        public int PushTracksCloseMax { get { return _maxattemps; } set { _maxattemps = value; } }

        bool _track = true;
        /// <summary>
        /// enable or disable tracking
        /// </summary>
        public bool TrackEnabled { get { return _track; } set { _track = value; } }
        BackgroundWorker _bw = new BackgroundWorker();
        bool _go = false;
        public event DebugDelegate SendDebug;
    }
}
