using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Threading;
using Twilio.Types;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Startbutton.Windows;
using System.Diagnostics;
using Twilio.Jwt.AccessToken;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace CryptoDexNotifier
{
    class DataManager : BaseManager
    {
        const string apiUrl = "https://public-api.dextools.io/trial/";
        const string apiKey = "mDWOwoIf6SaeOkLjl10Cp9U72PKzRjKEaatqMV04"; // Replace with your actual API key
        static Dictionary<int, List<decimal>> dicPrices = new Dictionary<int, List<decimal>>();
        static object lockToken = new object();
        static List<int> tokenIDs = new List<int>();
        static List<decimal> lastPrices = new List<decimal>();
        static List<decimal> last5Prices = new List<decimal>();

        const int DECREASE = 1;
        const int INCREASE = 2;

        public DataManager()
        {

        }
        public class PriceData
        {
            public Data data { get; set; }
            public class Data
            {
                public int tokenid { get; set; }
                public decimal price { get; set; }
                public decimal priceChain { get; set; }
                public decimal? variation5m { get; set; }
                public decimal? variationChain5m { get; set; }
                public decimal? variation1h { get; set; }
                public decimal? variationChain1h { get; set; }
                public decimal? variation6h { get; set; }
                public decimal? variationChain6h { get; set; }
                public decimal? variation24h { get; set; }
                public decimal? variationChain24h { get; set; }
            }
        }

        public static void SaveTokenPrice(SqlConnection con, int TokenModelId, decimal Price, DateTime FetchTime)
        {
            //SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["Main"].ToString());
            //con.Open();

            SqlCommand cmdInsert = new SqlCommand("add_token_price", con);
            cmdInsert.CommandType = CommandType.StoredProcedure;
            cmdInsert.CommandTimeout = 0;
            cmdInsert.Parameters.AddWithValue("@token_id", TokenModelId);
            cmdInsert.Parameters.AddWithValue("@price", Price);
            cmdInsert.Parameters.AddWithValue("@fetchtime", FetchTime);

            cmdInsert.CommandTimeout = 1800;
            cmdInsert.ExecuteNonQuery();
            //con.Close();
            System.Console.WriteLine("Saved Token Price");
        }
        public static void SendSms(string recipientPhoneNumber, string message)
        {
            // Your Twilio account SID and auth token
            string accountSid = "AC0452f7c2667fa6914ba7ac15f54c4b42";
            string authToken = "18656694d4b010ebfcf25fc5577c3b0d";

            // Initialize the Twilio client
            TwilioClient.Init(accountSid, authToken);

            try
            {
                // Send the SMS message using the Twilio API
                var sentMessage = MessageResource.Create(
                    body: message,
                    from: new PhoneNumber("+447723471060"),
                    to: new PhoneNumber(recipientPhoneNumber)
                );


            }
            catch (Exception ex)
            {
                // Handle any exceptions
                Console.WriteLine(ex.Message);
            }

        }
        public static bool SendEmail(string subject, string body, string email, string attach_path)
        {
            //string SERVER = "dedrelay.secureserver.net";
            //int PORT = 587;
            //bool SSL = false;
            //string ID = "systememail@revupcommerce.com";
            //string PASSWORD = "D8e#37!";

            string SERVER = "smtp.gmail.com";
            int PORT = 587;
            bool SSL = true;
            string ID = "revupsystem@gmail.com";
            string PASSWORD = "D8e#37!!!";

            try
            {
                SmtpClient smtp = new SmtpClient(SERVER);

                smtp.UseDefaultCredentials = true;
                smtp.EnableSsl = SSL;
                smtp.Port = PORT;
                smtp.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                smtp.Credentials = new NetworkCredential(ID, PASSWORD);
                smtp.Timeout = 100000;

                MailMessage mail = new MailMessage();
                mail.IsBodyHtml = true;
                mail.From = new MailAddress("automailer@amazon.revupcommerce.com", "RevUpCommerce Automailer");

                string[] a = email.Split(';');

                for (int x = 0; x < a.Length; x++)
                    mail.To.Add(new MailAddress(a[x]));

                //mail.To.Add(new MailAddress(email));
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = true;

                if (attach_path != null)
                {
                    Attachment item = new Attachment(attach_path);
                    mail.Attachments.Add(item);
                }

                smtp.Send(mail);
                return true;
            }
            catch (InvalidCastException err)
            {
                //AppendLog(LOG_EVENT.ONERRORMSG, "Issue in emailing : " + err);
                return false;
            }

            catch (System.Exception err)
            {
                //AppendLog(LOG_EVENT.ONERRORMSG, "Issue in emailing : " + err);
                return false;
            }
        }

        private static string CreateAlertContent(int type, float percent, decimal currentPrice, decimal lastPrice)
        {
            string alertText = "";

            decimal prcpercent = ((currentPrice - lastPrice) / lastPrice) * 100;
            if (Math.Abs(prcpercent) >= (decimal)percent)
            {
                if (type == DECREASE)
                {
                    alertText = "Current price(" + $"{currentPrice}" + ") is decreased by " + Math.Round(prcpercent, 4) + "%";
                }
                else if (type == INCREASE)
                {
                    alertText = "Current price(" + $"{lastPrice}" + ") is increased by " + Math.Round(prcpercent,  4) + "%";
                }
            }

            return alertText;
        }

        private static string CreateAlertContent(int type, decimal LimitPrice, decimal currentPrice)
        {
            string alertText = "";

            if (type == DECREASE && LimitPrice >= currentPrice)
            {
                alertText = alertText + "\nCurrent price(" + $"{currentPrice}) is Less than or equal to your setting value({LimitPrice}).";
            }
            else if (type == INCREASE && LimitPrice <= currentPrice)
            {
                alertText = alertText + "\nCurrent price(" + $"{currentPrice}) is Greater than or equal to your setting value({LimitPrice}).";
            };

            return alertText;
        }

        public static async Task<decimal?> GetPricByApi(string chain, string tokenname, string address)
        {
            chain = chain.Trim();
            address = address.Trim();
            //System.Console.WriteLine(chain + ", " + tokenname + ", " + address);

            string url = apiUrl + "v2/token/" + chain + "/" + address + "/price";
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("accept", "application/json");
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);

                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();

                    var result = JsonConvert.DeserializeObject<PriceData>(content);
                    if (result != null && result.data != null)
                    {
                        decimal priceChainValue = 1 / (decimal)result.data.priceChain;
                        decimal priceValue = result.data.price;
                        
                        //System.Console.WriteLine("Price: " + priceValue.ToString());

                        return priceValue;
                    }
                }
            }

            return null;
        }

        static int AddAlertToken(int TokenID, decimal PriceLast)
        {
            dicPrices.TryGetValue(TokenID, out var prices);

            if (prices == null)
            {
                prices = new List<decimal>();

                prices.Add(PriceLast);
                dicPrices.Add(TokenID, prices);
            }

            while (prices.Count > 5)
                prices.Remove(0);

            lock (lockToken)
            {
                tokenIDs.Add(TokenID);
                lastPrices.Add(PriceLast);
                last5Prices.Add(prices[0]);
            }

            return tokenIDs.Count;
        }

        public static void TokenDexAPI()
        {
            SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["Main"].ToString());
            con.Open();

            SqlCommand cmdSelect = new SqlCommand("SELECT Tokens.* FROM Tokens join (select distinct TokenID from TokenModel) m on (Tokens.id=m.TokenID) order by Tokens.Id", con);

            SqlDataAdapter sda = new SqlDataAdapter(cmdSelect);
            DataTable dt = new DataTable();

            sda.Fill(dt);

            foreach (DataRow dbrow in dt.Rows)
            {
                int tokenid = (int)dbrow[0];
                string chainname = dbrow[1].ToString();
                string tokenname = dbrow[2].ToString();
                string address = dbrow[3].ToString();


                DateTime now = DateTime.Now;

                decimal? price = GetPricByApi(chainname, tokenname, address).GetAwaiter().GetResult();

                if (price != null)
                {
                    SaveTokenPrice(con, tokenid, price.Value, now);
                    AddAlertToken(tokenid, price.Value);
                }

                if (price == null)
                    continue;
            }
            
            con.Close();
        }

        public static int SendAlert()
        {
            if (tokenIDs.Count == 0)
                return 0;

            int tokenID;
            decimal lastPrice, last5Price;
            
            lock(lockToken)
            {
                tokenID = tokenIDs[0];
                lastPrice = lastPrices[0];
                last5Price = last5Prices[0];

                tokenIDs.RemoveAt(0);
                lastPrices.RemoveAt(0);
                last5Prices.RemoveAt(0);
            }

            SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["Main"].ToString());
            con.Open();

            SqlCommand cmdSelect = new SqlCommand(
                $"SELECT AlertSettingModel.*,TokenModel.UserId FROM AlertSettingModel join TokenModel on (AlertSettingModel.TokenModelId=TokenModel.ID) where TokenModel.TokenID={tokenID} order by AlertSettingModel.Id", con);

            SqlDataAdapter sda = new SqlDataAdapter(cmdSelect);
            DataTable dt = new DataTable();

            sda.Fill(dt);

            foreach(DataRow dbrow in dt.Rows)
            {
                string Alert = "";

                if (0 != (dbrow.Field<int?>("PriceChange") ?? 0))
                {
                    Alert = CreateAlertContent(dbrow.Field<int>("PriceChange"), dbrow.Field<float>("PriceChangePercent"), lastPrice, last5Price);
                }

                if (0 != (dbrow.Field<int?>("PriceIs") ?? 0))
                {
                    Alert += CreateAlertContent(dbrow.Field<int>("PriceIs"), dbrow.Field<decimal>("PriceIsValue"), lastPrice);
                }

                if (!Alert.IsNullOrEmpty())
                {
                    GetMainAndPhone(con, dbrow.Field<string>("UserId"), out string Email, out string Phone);

                    SendEmail(con, dbrow.Field<int>("Id"), Email, Alert, dbrow.Field<string>("AlertText"));
                    SendPhone(con, dbrow.Field<int>("Id"), Phone, Alert, dbrow.Field<string>("AlertText"));
                }
            }

            con.Close();
            return 1;
        }

        static bool GetMainAndPhone(SqlConnection con, string UserID, out string Mail, out string Phone)
        {
            SqlCommand cmdSelect = new SqlCommand(
                $"SELECT * FROM AspNetUsers where Id={UserID}", con);

            SqlDataAdapter sda = new SqlDataAdapter(cmdSelect);
            DataTable dt = new DataTable();

            sda.Fill(dt);

            Mail = "";
            Phone = "";

            foreach (DataRow dbrow in dt.Rows)
            {
                Mail = dbrow.Field<string>("Email");
                Phone = dbrow.Field<string>("PhoneNumber");

                return true;
            }

            return false;
        }

        static int SendEmail(SqlConnection con, int AlerID, string MainEmail, string Alert1, string Alert2)
        {
            int cnt = 0;

            if(!MainEmail.IsNullOrEmpty())
            {
                cnt++;
            }

            SqlCommand cmdSelect = new SqlCommand(
                $"SELECT EmailModel.email FROM AlertSettingMail join EmailModel on (AlertSettingMail.MailID=EmailModel.ID) where AlertSettingMail.AlertID={AlerID} order by AlertSettingMail.Id", con);

            SqlDataAdapter sda = new SqlDataAdapter(cmdSelect);
            DataTable dt = new DataTable();

            sda.Fill(dt);

            foreach(DataRow dbrow in dt.Rows)
            {
                SendEmail(Alert2, dbrow.Field<string>(0), Alert1, "");
                cnt++;
            }

            return cnt;
        }

        static int SendPhone(SqlConnection con, int AlerID, string MainPhone, string Alert1, string Alert2)
        {
            int cnt = 0;

            if (!MainPhone.IsNullOrEmpty())
            {
                cnt++;
            }

            SqlCommand cmdSelect = new SqlCommand(
                $"SELECT PhoneModel.phoneNumber FROM AlertSettingPhone join PhoneModel on (AlertSettingPhone.PhoneID=PhoneModel.ID) where AlertSettingPhone.AlertID={AlerID} order by AlertSettingPhone.Id", con);

            SqlDataAdapter sda = new SqlDataAdapter(cmdSelect);
            DataTable dt = new DataTable();

            sda.Fill(dt);

            foreach (DataRow dbrow in dt.Rows)
            {
                SendSms(dbrow.Field<string>(0), Alert2 + "\n" + Alert1);
                cnt++;
            }

            return cnt;
        }


        private string EmailNotifierRootDir = AppDomain.CurrentDomain.BaseDirectory + "EmailReport";

        public string Generate(string file_name, StringBuilder sb)
        {
            try
            {
                if (!Directory.Exists(EmailNotifierRootDir))
                    Directory.CreateDirectory(EmailNotifierRootDir);

                string file_path = EmailNotifierRootDir + "\\" + file_name;
                if (File.Exists(file_path))
                    File.Delete(file_path);

                if (File.Exists(file_path))
                    throw new Exception(string.Format("Delete file error: {0}", file_path));

                using (StreamWriter sw = new StreamWriter(file_path))
                {
                    sw.Write(sb);
                }

                return file_path;
            }
            catch (Exception ex)
            {
                AppendLog(LOG_EVENT.ONERRORMSG, "Issue in Writting File: " + ex);

                return string.Empty;
            }
        }

        //public static bool Send(string subject, string body, string email, string attach_path)
        //{
        //    MailMessage msg = new MailMessage();
        //    msg.From = new MailAddress("automailer@amazon.revupcommerce.com", "RevUpCommerce Automailer");

        //    string[] a = email.Split(';');

        //    for (int x = 0; x < a.Length; x++)
        //        msg.To.Add(new MailAddress(a[x]));

        //    msg.Subject = subject;
        //    msg.IsBodyHtml = false;
        //    msg.Body = body;

        //    Attachment item = new Attachment(attach_path);
        //    msg.Attachments.Add(item);

        //    Exception ex = new Exception();

        //    bool res = Startbutton.Library.SendMail(msg, ref ex, false);

        //    return res;
        //}

        public bool Send(string subject, string body, string email, string attach_path)
        {
            //string SERVER = "dedrelay.secureserver.net";
            //int PORT = 587;
            //bool SSL = false;
            //string ID = "systememail@revupcommerce.com";
            //string PASSWORD = "D8e#37!";

            string SERVER = "smtp.gmail.com";
            int PORT = 587;
            bool SSL = true;
            string ID = "revupsystem@gmail.com";
            string PASSWORD = "D8e#37!!!";

            try
            {
                SmtpClient smtp = new SmtpClient(SERVER);

                smtp.UseDefaultCredentials = true;
                smtp.EnableSsl = SSL;
                smtp.Port = PORT;
                smtp.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                smtp.Credentials = new NetworkCredential(ID, PASSWORD);
                smtp.Timeout = 100000;

                MailMessage mail = new MailMessage();
                mail.IsBodyHtml = true;
                mail.From = new MailAddress("automailer@amazon.revupcommerce.com", "RevUpCommerce Automailer");

                string[] a = email.Split(';');

                for (int x = 0; x < a.Length; x++)
                    mail.To.Add(new MailAddress(a[x]));

                //mail.To.Add(new MailAddress(email));
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = true;

                if (attach_path != null)
                {
                    Attachment item = new Attachment(attach_path);
                    mail.Attachments.Add(item);
                }

                smtp.Send(mail);

                return true;
            }
            catch (InvalidCastException err)
            {
                AppendLog(LOG_EVENT.ONERRORMSG, "Issue in emailing : " + err);

                return false;
            }

            catch (System.Exception err)
            {
                AppendLog(LOG_EVENT.ONERRORMSG, "Issue in emailing : " + err);

                return false;
            }
        }

        public void RunSalesPerson()
        {
            this.bError = false;
            string header = "\"Vendor Name\",\"Order ID\",\"Tracking No\"";

            DateTime start_date = DateTime.Now;

            string filename = string.Format("SalesPersonReport_{0}.csv",
               start_date.ToString("MM-dd-yy"));

            try
            {
                AppendLog(LOG_EVENT.ONNOTICEMSG, string.Format("Calling procedure.. [{0}]", "salesperson_notification"));

                DataTable dt = new DataTable();

                int nRep = 5;
                while (nRep > 0)
                {
                    try
                    {
                        SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["Main"].ToString());
                        SqlCommand cmd = new SqlCommand("salesperson_notification", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandTimeout = 0;

                        SqlDataAdapter sda = new SqlDataAdapter(cmd);

                        sda.Fill(dt);
                    }
                    catch (InvalidOperationException ex)
                    {
                        nRep--;
                        Thread.Sleep(1000);
                        continue;
                    }

                    break;
                }

                int i = 0;
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(header);
                string email = "";
                string firstName = "";
                string lastName = "";
                string vendorName = "";
                string trackingNo = "";
                string vendorOrderID = "";
                string id = "";
                while (i < dt.Rows.Count)
                {
                    vendorOrderID = (string)dt.Rows[i][0];
                    trackingNo = (string)dt.Rows[i][1];
                    vendorName = (string)dt.Rows[i][3];
                    firstName = (string)dt.Rows[i][4];
                    lastName = (string)dt.Rows[i][5];
                    if (i == 0)
                    {
                        email = (string)dt.Rows[i][6];
                    }
                    id = ((Guid)dt.Rows[i][7]).ToString();

                    if (email != (string)dt.Rows[i][6] || i == dt.Rows.Count - 1)
                    {
                        if( i == 0 )
                            sb.AppendLine("\"" + vendorName + "\",\"" + vendorOrderID + "\",\"" + firstName + " " + lastName + "\"");

                        string filepath = Generate(filename, sb);
                        string textbody;

                        textbody = string.Format("Dear {0} {1}, The following order(s) have been marked as delivered to you, however we have Not yet received them in our warehouse. Please check that you DID in fact receive these orders. If not, please contact us asap so we can research furthur.", firstName, lastName);
                        textbody = textbody + "\n" + sb.ToString();

                        Send(
                            string.Format("Sales Person Notification {0}", start_date.ToString("MM-dd-yy")),
                            textbody,
                            string.Format("{0};{1}", "jay@revupcommerce.com", email),
                            null
                            );

                        sb.Clear();
                        sb.Append(header);
                    }
                    sb.AppendLine("\"" + vendorName + "\",\"" + vendorOrderID + "\",\"" + firstName + " " + lastName + "\"");

                    try
                    {
                        SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["Main"].ToString());
                        con.Open();
                        String updateSQL = "UPDATE PurchaseOrders SET NotificationSend=1 WHERE ID='" + id + "'";
                        SqlCommand updateCmd = new SqlCommand(updateSQL, con);
                        updateCmd.CommandType = CommandType.Text;
                        updateCmd.CommandTimeout = 1800;
                        updateCmd.ExecuteNonQuery();
                        con.Close();
                    }
                    catch (Exception e)
                    { 
                    }

                    i++;
                }

            }
            catch (Exception e)
            {
                AppendLog(LOG_EVENT.ONERRORMSG, "Stored Proc call issue: " + e);
            }

            try
            {
                SetServiceError(this.bError);
            }
            catch (Exception)
            {

            }
        }

        //public static async void ApiFunction(SqlConnection con, string userid, int tokenid, string chain, string tokenname, string address, bool alert, string last)
        //{
        //    chain = chain.Trim();
        //    address = address.Trim();
        //    System.Console.WriteLine(chain + ", " + tokenname + ", " + address);
        //    string apiUrl = "https://public-api.dextools.io/trial/";
        //    string apiKey = "mDWOwoIf6SaeOkLjl10Cp9U72PKzRjKEaatqMV04"; // Replace with your actual API key

        //    string url = apiUrl + "v2/token/" + chain + "/" + address + "/price";
        //    using (HttpClient client = new HttpClient())
        //    {
        //        client.DefaultRequestHeaders.Add("accept", "application/json");
        //        client.DefaultRequestHeaders.Add("x-api-key", apiKey);

        //        HttpResponseMessage response = await client.GetAsync(url);

        //        if (response.IsSuccessStatusCode)
        //        {
        //            string content = await response.Content.ReadAsStringAsync();

        //            var result = JsonConvert.DeserializeObject<PriceData>(content);
        //            if (result != null && result.data != null)
        //            {
        //                decimal priceChainValue = 1 / (decimal)result.data.priceChain;
        //                decimal priceValue = result.data.price;
        //                System.Console.WriteLine("Price: " + priceValue.ToString());
        //                SaveTokenPrice(con, userid, tokenid.ToString(), priceValue, priceChainValue.ToString(), DateTime.Now);
        //                if (alert)
        //                {
        //                    decimal lastPrice = 0;
        //                    SqlCommand cmdSettingSel = new SqlCommand("SELECT * FROM AlertSettingModel WHERE UserId='" + userid + "'", con);
        //                    SqlDataReader reader0 = cmdSettingSel.ExecuteReader();
        //                    string priceChange = "";
        //                    float PriceChangePercent = 0;
        //                    string PriceIs = "";
        //                    float priceIsValue = 0;
        //                    string alertText = "";

        //                    while (reader0.Read())
        //                    {
        //                        priceChange = reader0.GetString(1);
        //                        PriceChangePercent = reader0.GetFloat(2);
        //                        PriceIs = reader0.GetString(3);
        //                        priceIsValue = reader0.GetFloat(4);
        //                        alertText = reader0.GetString(5);
        //                        break;
        //                    }
        //                    List<decimal> priceList = new List<decimal>();
        //                    SqlCommand cmdPriceListSel = new SqlCommand("SELECT * FROM TokenPriceModel WHERE UserId='" + userid + "'", con);
        //                    SqlDataReader reader1 = cmdPriceListSel.ExecuteReader();
        //                    while (reader1.Read())
        //                    {
        //                        priceList.Add(reader1.GetDecimal(3));
        //                    }
        //                    SqlCommand cmdEmailSel = new SqlCommand("SELECT * FROM EmailModel WHERE alert=1 AND userId='" + userid + "'", con);
        //                    using (SqlDataReader reader2 = cmdEmailSel.ExecuteReader())
        //                    {
        //                        // Check if the reader has rows
        //                        if (reader2.HasRows)
        //                        {
        //                            // Iterate through the rows and retrieve the data
        //                            while (reader2.Read())
        //                            {
        //                                string email = reader2.GetString(1);
        //                                System.Console.WriteLine(email);
        //                                if (priceList.Count > 0) lastPrice = priceList[priceList.Count - 1];
        //                                alertText = CreateAlertContent(PriceChangePercent, priceIsValue, $"{priceChainValue}", last);
        //                                if (alertText == "") SendEmail("For trading", alertText, email, "");
        //                            }
        //                        }
        //                    }
        //                    SqlCommand cmdPhoneSel = new SqlCommand("SELECT * FROM PhoneModel WHERE alert=1 AND userId='" + userid + "'", con);
        //                    SqlDataReader reader3 = cmdPhoneSel.ExecuteReader();
        //                    while (reader3.Read())
        //                    {
        //                        string phoneNumber = reader3.GetString(1);
        //                        alertText = CreateAlertContent(PriceChangePercent, priceIsValue, $"{priceChainValue}", last);
        //                        if (alertText == "") SendSms(phoneNumber, alertText);
        //                    }
        //                }
        //            }
        //        }
        //        else
        //        {
        //            Console.WriteLine($"Request failed with status code: {response.StatusCode}");
        //        }
        //    }
        //}
    }

    class ProcData
    {
        public string attach_path;
        public string asin_msg;
    }
}
