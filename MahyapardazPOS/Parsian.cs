using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Intek.PcPosLibrary;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Threading;
using System.Collections.Specialized;

namespace PAX.PCPOS.WinSample
{
    class Parsian
    {
        private Dictionary<string, string> httpResponse;
        LogWriter loger = new LogWriter("");
        string lastError = "";

        public Parsian(Dictionary<string, string> httpResponse)
        {

            this.httpResponse = new Dictionary<string, string>();
            this.httpResponse = httpResponse;

        }

        private Dictionary<int, SerialPort> Coms = new Dictionary<int, SerialPort>();
        private Dictionary<int, bool> ComStatus = new Dictionary<int, bool>();

        public Dictionary<string, string> response = new Dictionary<string, string>();

        public int COM_Index;

        public void payMoney(string AM , string COM)
        {
            BTLV tlv = new BTLV();
            tlv.AddEntry("PR", "00000");
            tlv.AddEntry("AM", AM);
            tlv.AddEntry("CU", "364");
            tlv.AddEntry("R1", "");
            tlv.AddEntry("R2", "");
            tlv.AddEntry("T1", "");
            tlv.AddEntry("T2", "");
            tlv.AddEntry("SV", "");
            tlv.AddEntry("SG", "");
            tlv.AddEntry("AD", "");
            tlv.AddEntry("PD", "1");


            String gg = tlv.ToString();
            tlv = new BTLV();
            tlv.AddEntry("RQ", gg);
            String txt_req = tlv.ToString();
            byte[] bb = ASCIIEncoding.GetEncoding(1256).GetBytes(txt_req.Length.ToString().PadLeft(4, '0') + txt_req);

            string[] COMS = new string[] { "COM1", "COM2", "COM3", "COM4", "COM5", "COM6" };
            COM_Index = Array.IndexOf(COMS, COM);

            if (!ComStatus.ContainsKey(COM_Index))
            {
                ComStatus[COM_Index] = false;
                Coms[COM_Index] = new SerialPort(COMS[COM_Index], 19200, Parity.None, 8, StopBits.One);
            }

            if (!ComStatus[COM_Index])
            {


                //if (!Coms[COM_Index].IsOpen)
                try {
                    Coms[COM_Index].Open();
                    loger.LogWrite("Open Port " + COMS[COM_Index]);
                }
                catch(Exception e)
               {
                    lastError = "Com Port " + (COM_Index + 1) + " Can not Be Openned.";
                    loger.LogWrite(lastError);
                    loger.LogWrite(e.Message);
                    return;
                }
                

                try
                {
                    bb = ASCIIEncoding.GetEncoding(1256).GetBytes(txt_req.Length.ToString().PadLeft(4, '0') + txt_req);
                    Coms[COM_Index].DiscardInBuffer();
                    Coms[COM_Index].DiscardOutBuffer();
                    Coms[COM_Index].ReadTimeout = 120000;
                    Coms[COM_Index].Write(bb, 0, bb.Length);

                }
                catch
                {
                    lastError = "Com Port " + (COM_Index + 1) + " Data Can not Be Sent.";
                    loger.LogWrite(lastError);
                    return;
                }

                byte[] ll = new byte[4];
                byte[] ss = new byte[500];

                try
                {

                    Coms[COM_Index].ReceivedBytesThreshold = 4;
                    int tt = 0;
                    while (true)
                    {
                        int cc = Coms[COM_Index].Read(ll, tt, 4 - tt);
                        tt += cc;
                        if (tt == 4)
                            break;
                    }
                    int len = int.Parse(ASCIIEncoding.ASCII.GetString(ll));
                    Coms[COM_Index].ReceivedBytesThreshold = len;
                    ss = new byte[len];
                    tt = 0;
                    while (true)
                    {
                        int cc = Coms[COM_Index].Read(ss, tt, len - tt);
                        tt += cc;
                        if (tt == len)
                            break;
                    }
                    string s1 = ASCIIEncoding.GetEncoding(1256).GetString(ss);
                    //Console.WriteLine("_________________________________________________________");
                    //Console.WriteLine(new BTLV().Open(s1.Substring(5)).GetResponse()["RS"]);


                    response = new BTLV().Open(s1.Substring(5)).GetResponse();
                    Coms[COM_Index].Close();

                }
                catch (Exception ex)
                {
                    lastError = "Com Port " + (COM_Index + 1) + " Data Can not Be Receive.";
                    loger.LogWrite(lastError);
                }
            }

            Coms[COM_Index].RtsEnable = false;
            Coms[COM_Index].DtrEnable = false;
            ComStatus[COM_Index] = true;
        }

        public Dictionary<string, string> pay(NameValueCollection query)
        {

            payMoney(query.Get("Amount"), query.Get("Port"));

            //dictionary = pos.Receive;
            Dictionary<string, string> posResponse = response;

            // this code is for error massage from open or close port
            if (response.Count == 0)
                httpResponse.Add("ErrorMessage", lastError);

            httpResponse.Add("ErrorCode", "0");
            httpResponse.Add("WarningCode", "0");
            httpResponse.Add("MerchantId", "");
            httpResponse.Add("SerialNumber", "");
            httpResponse.Add("MerchantName", "");

            if (posResponse.ContainsKey("RS") && posResponse["RS"] == "00")
                httpResponse.Add("Success", "True");
            else
                httpResponse.Add("Success", "False");

            if (posResponse.ContainsKey("RS")) {
                httpResponse.Add("ResponseCode", posResponse["RS"]);
                httpResponse.Add("ErrorMessage", translateError(posResponse["RS"]));

                loger.LogWrite(translateError(posResponse["RS"]));
            }
               

            if (posResponse.ContainsKey("AM"))
                httpResponse.Add("Amount", posResponse["AM"]);
            else
                httpResponse.Add("Amount", "");

            if (posResponse.ContainsKey("TM"))
                httpResponse.Add("TerminalId", posResponse["TM"]);
            else
                httpResponse.Add("TerminalId", "");

            if (posResponse.ContainsKey("TI"))
                httpResponse.Add("DateTime", posResponse["TI"]);
            else
                httpResponse.Add("DateTime", "");

            if (posResponse.ContainsKey("PN"))
                httpResponse.Add("PAN", posResponse["PN"]);
            else
                httpResponse.Add("PAN", "");

            if (posResponse.ContainsKey("RN"))
                httpResponse.Add("RRN", posResponse["RN"]);
            else
                httpResponse.Add("RRN", "");

            if (posResponse.ContainsKey("TR"))
                httpResponse.Add("Stan", posResponse["TR"]);
            else
                httpResponse.Add("Stan", "");

            if (posResponse.ContainsKey("SI"))
                httpResponse.Add("SVC", posResponse["SI"]);
            else
                httpResponse.Add("SVC", "");

            return httpResponse;

        }

        public string translateError(string errorCode)
        {
            string text = "خطا در پرداخت:";

            int errorValue = int.Parse(errorCode);

            switch (errorValue)
            {
                case 12:
                    text += "تراکنش نامعتبر است.";
                    break;
                case 50:
                    text += "عدم برقراری ارتباط با مرکز.";
                    break;
                case 51:
                    text += "موجودی کافی نمی باشد.";
                    break;
                case 54:
                    text += "تاریخ انقضای کارت سپری شده است.";
                    break;
                case 55:
                    text += "رمز کارت اشتباه است.";
                    break;
                case 56:
                    text += "کارت نامعتبر است.";
                    break;
                case 58:
                    text += " .پایانه غیر مجاز است.";
                    break;
                case 61:
                    text += "مبلغ تراکنش بیش از حد مجاز می باشد.";
                    break;
                case 65:
                    text += "تعداد دفعات ورود رمز غلط بیش از حد مجاز است.";
                    break;
                case 75:
                    text += "رمز کارت اشتباه است.";
                    break;
                case 99:
                    text += "لغو درخواست.";
                    break;

                default:
                    text += "..";
                    break;
            }

            return text;
        }




    }
}
