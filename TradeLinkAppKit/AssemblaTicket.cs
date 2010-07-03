﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Xml;
using System.ServiceModel.Web;
using TradeLink.API;
using TradeLink.Common;
namespace TradeLink.AppKit
{
    /// <summary>
    /// to create and update assembla tickets
    /// </summary>
    public class AssemblaTicket
    {
        public static string GetTicketsUrl(string space)
        {
            return "http://www.assembla.com/spaces/" + space + "/tickets";
        }
        /// <summary>
        /// returns global id of ticket if successful, zero if not successful
        /// (global id does not equal space's ticket id)
        /// </summary>
        /// <param name="space"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="summary"></param>
        /// <returns></returns>
        public static int Create(string space, string user, string password, string summary) { return Create(space, user, password, summary, string.Empty, TicketStatus.New, Priority.Normal); }
        public static int Create(string space, string user, string password, string summary, string description, TicketStatus status, Priority priority)
        {
            int stat = (int)status;
            int pri = (int)priority;
            string url = GetTicketsUrl(space);
            HttpWebRequest hr = WebRequest.Create(url) as HttpWebRequest;
            hr.Credentials = new System.Net.NetworkCredential(user, password);
            hr.PreAuthenticate = true;
            hr.Method = "POST";
            hr.ContentType = "application/xml";
            StringBuilder data = new StringBuilder();
            data.AppendLine("<ticket>");
            data.AppendLine("<status>"+stat.ToString()+"</status>");
            data.AppendLine("<priority>" + pri.ToString()+"</priority>");
            data.AppendLine("<summary>");
            data.AppendLine(System.Web.HttpUtility.HtmlEncode(summary));
            data.AppendLine("</summary>");
            data.AppendLine("<description>");
            data.AppendLine(System.Web.HttpUtility.HtmlEncode(description));
            data.AppendLine("</description>");
            data.AppendLine("</ticket>");
            // encode
            byte[] bytes = UTF8Encoding.UTF8.GetBytes(data.ToString());
            hr.ContentLength = bytes.Length;
            // prepare id
            int id = 0;
            try
            {
                // write it
                System.IO.Stream post = hr.GetRequestStream();
                post.Write(bytes, 0, bytes.Length);
                // get response
                System.IO.StreamReader response = new System.IO.StreamReader(hr.GetResponse().GetResponseStream());
                // get string version
                string rs = response.ReadToEnd();

                XmlDocument xd = new XmlDocument();
                xd.LoadXml(rs);
                XmlNodeList xnl = xd.GetElementsByTagName("id");
                string val = xnl[0].InnerText;
                if ((val!=null) && (val!=string.Empty))
                    id = Convert.ToInt32(val);
                // display it
                if (SendDebug!=null)
                    SendDebug(DebugImpl.Create(rs));
            }
            catch (Exception ex) 
            {
                if (SendDebug != null)
                    SendDebug(DebugImpl.Create("exception: " + ex.Message+ex.StackTrace)); 
                return 0; 
            }
            return id;
        }

        /// <summary>
        /// see https://www.assembla.com/wiki/show/breakoutdocs/Ticket_REST_API
        /// for example of valid xml updates
        /// </summary>
        /// <param name="space"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="ticket"></param>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static bool UpdateComment(string space, string user, string password, int ticket, string comment)
        {
            string xml = "<user-comment>" + comment + "</user-comment>";
            return Update(space,user,password,ticket,xml);
        }

        public static bool UpdateStatus(string space, string user, string password, int ticket, TicketStatus status)
        {
            int stat = (int)status;
            string xml = "<status>"+stat.ToString()+"</status>";
            return Update(space,user,password,ticket,xml);
        }
        public static bool Update(string space, string user, string password, int ticket, string xml)
        {
            string url = "http://www.assembla.com/spaces/" + space + "/tickets/"+ticket.ToString();
            HttpWebRequest hr = WebRequest.Create(url) as HttpWebRequest;
            hr.Credentials = new System.Net.NetworkCredential(user, password);
            hr.Method = "PUT";
            hr.ContentType = "application/xml";
            StringBuilder data = new StringBuilder();
            data.AppendLine(System.Web.HttpUtility.HtmlEncode(xml));
            // encode
            byte[] bytes = UTF8Encoding.UTF8.GetBytes(data.ToString());
            hr.ContentLength = bytes.Length;
            try
            {
                // write it
                System.IO.Stream post = hr.GetRequestStream();
                post.Write(bytes, 0, bytes.Length);
                // get response
                System.IO.StreamReader response = new System.IO.StreamReader(hr.GetResponse().GetResponseStream());
                // display it
                if (SendDebug != null)
                    SendDebug(DebugImpl.Create(response.ReadToEnd()));
            }
            catch (Exception ex)
            {
                if (SendDebug != null)
                    SendDebug(DebugImpl.Create("exception: " + ex.Message + ex.StackTrace));
                return false;
            }
            return true;
        }

        private string _space = string.Empty;
        public string Space { get { return _space; } set { _space = value; } }
        private string _un = string.Empty;
        public string Username { get { return _un; } set { _un = value; } }
        internal string _pw = string.Empty;
        public string Password { set { _pw = value ; } }

        string _sum = string.Empty;
        public string Summary { get { return _sum; } set { _sum = value; } }
        string _desc = string.Empty;
        public string Description { get { return _desc; } set { _desc = value; } }
        Priority _pri = Priority.Normal;
        public Priority Priority { get { return _pri; } set { _pri = value; } }
        TicketStatus _stat = TicketStatus.New;
        public TicketStatus Status { get { return _stat; } set { _stat = value; } }

        public static event DebugFullDelegate SendDebug;

        public override string ToString()
        {
            return Summary;
        }

        /// <summary>
        /// create a new assembla ticket
        /// </summary>
        /// <param name="summary"></param>
        /// <param name="desc"></param>
        public AssemblaTicket(string summary, string desc) : this(summary, desc, Priority.Normal, TicketStatus.New) { }

        /// <summary>
        /// create a new assembla ticket
        /// </summary>
        /// <param name="summary"></param>
        /// <param name="desc"></param>
        /// <param name="pri"></param>
        /// <param name="stat"></param>
        public AssemblaTicket(string summary, string desc, Priority pri, TicketStatus stat)
        {
            _stat = stat;
            _pri = pri;
            _sum = summary;
            _desc = desc;
        }
        /// <summary>
        /// create a new assembla ticket in a given space using a given account
        /// </summary>
        /// <param name="space"></param>
        /// <param name="username"></param>
        /// <param name="pw"></param>
        /// <param name="summary"></param>
        /// <param name="desc"></param>
        /// <param name="pri"></param>
        /// <param name="stat"></param>
        public AssemblaTicket(string space, string username, string pw,string summary, string desc, Priority pri, TicketStatus stat)
        {
            _space = space;
            _un = username;
            _pw = pw;
            _stat = stat;
            _pri = pri;
            _sum = summary;
            _desc = desc;
        }

        /// <summary>
        /// get extra information about this machine (formatted)
        /// </summary>
        /// <returns></returns>
        public static string TicketContext() { return TicketContext("?", "?", null); }

        /// <summary>
        /// get extra information about this machine (formatted)
        /// </summary>
        /// <param name="program"></param>
        /// <returns></returns>
        public static string TicketContext(string program) { return TicketContext(program, program, null); }

        /// <summary>
        /// get extra information about this machine (formatted)
        /// </summary>
        /// <param name="program"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static string TicketContext(string program, Exception ex) { return TicketContext(program, program, ex); }

        /// <summary>
        /// get a formatted description of information about this machine
        /// </summary>
        /// <param name="space"></param>
        /// <param name="program"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static string TicketContext(string space, string program, Exception ex)
        {
            string[] r = new string[] { "Product:" + space, "Program:" + program, "Exception:" + (ex != null ? ex.Message : "n/a"), "StackTrace:" + (ex != null ? ex.StackTrace : "n/a"), "CommandLine:" + Environment.CommandLine, "OS:" + Environment.OSVersion.VersionString + " " + (IntPtr.Size * 8).ToString() + "bit", "CLR:" + Environment.Version.ToString(4), "TradeLink:" + TradeLink.Common.Util.TLSIdentity(), "Memory:" + Environment.WorkingSet.ToString(), "Processors:" + Environment.ProcessorCount.ToString(), "MID: "+Auth.GetCPUId() };
            string desc = string.Join(Environment.NewLine, r);
            return desc;
        }
    }


}
