using bookfly_cips.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.Xml;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace bookfly_cips.Controllers
{
    public class HomeController : Controller
    {
        string validateTxnid;
        string validateTxnAmt;
        string validateToken;

        private readonly HttpClient _httpClient;

        string pfxPassword = "123";

        public HomeController()
        {
            _httpClient = new HttpClient();
        }

      
        string GenerateConnectIPSToken(string stringToHash, string pfxPassword)
        {
            try
            {
                string Filename = "E:\\ConnectIPS\\Documents\\Creditor.pfx";     //location of creditor.pfx key
                using (var crypt = new SHA256Managed())
                using (var cert = new X509Certificate2(Filename, pfxPassword, X509KeyStorageFlags.Exportable))
                {
                    byte[] data = Encoding.UTF8.GetBytes(stringToHash);

                    RSA csp = null;
                    if (cert != null)
                    {
                        csp = cert.PrivateKey as RSA;
                    }

                    if (csp == null)
                    {
                        throw new Exception("No valid cert was found");
                    }

                    csp.ImportParameters(csp.ExportParameters(true));
                    byte[] signatureByte = csp.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                    string tokenStringForReference = Convert.ToBase64String(signatureByte);

                    return tokenStringForReference;
                }
            }
            catch (Exception ex)
            {
                return "Error";
            }
        }

        public async Task<IActionResult> Index(float txnamt, string remarks, string particulars, int x)
        {
            if (x == 1)
            {
                int MERCHANTID = 1043;
                string APPID = "MER-1043-APP-1";
                string APPNAME = "BookandFlyTours";

                Guid guid = Guid.NewGuid();
                string guidString = Convert.ToBase64String(guid.ToByteArray())
                                       .Substring(0, 15)
                                       .Replace('/', '_')
                                       .Replace('+', '-');
                string TXNID = "txn-" + guidString;

                DateTime currentDate = DateTime.Now;
                string TXNDATE = currentDate.ToString("dd-MM-yyyy");

                
                //Must be integer according to documentation
                string TXNCRNCY = "NPR";
                float TXNAMT = txnamt;

                
                
                
                
                string validateTxnAmt = TXNAMT.ToString();
                string validateTxnAmtModified = validateTxnAmt + ".00"; 

                

            


                string REFERENCEID = "ref-" + guidString;
                string REMARKS = remarks;
                string PARTICULARS = particulars;

               
                //First token generation for transaction initiation
                string stringToHash = "MERCHANTID=" + MERCHANTID.ToString() + "," + "APPID=" + APPID + "," + "APPNAME=" + APPNAME + "," + "TXNID=" + TXNID + "," + "TXNDATE=" + TXNDATE + "," + "TXNCRNCY=" + TXNCRNCY + "," + "TXNAMT=" + TXNAMT.ToString() + "," + "REFERENCEID=" + REFERENCEID + "," + "REMARKS=" + REMARKS + "," + "PARTICULARS=" + PARTICULARS + "," + "TOKEN=TOKEN";

                Console.WriteLine("String to Hash : ",stringToHash);
                string convertedtoken = GenerateConnectIPSToken(stringToHash, pfxPassword);

                
                //For validation token generation
                string validateStringToHash = "MERCHANTID=1043" + "," + "APPID=MER-1043-APP-1" + "," + "REFERENCEID=" + TXNID + "," + "TXNAMT=" + validateTxnAmtModified;
                string validateToken = GenerateConnectIPSToken(validateStringToHash, "123");

                TempData["validateTxnid"] = TXNID;
                TempData["validateTxnAmt"] = validateTxnAmtModified;
                TempData["validateToken"] = validateToken;

            


               

                ViewBag.Merchantid = MERCHANTID;
                ViewBag.Appid = APPID;
                ViewBag.Appname = APPNAME;
                ViewBag.Txnid = TXNID;
                ViewBag.Txndate = TXNDATE;
                ViewBag.Txncrncy = TXNCRNCY;
                ViewBag.Txnamt = TXNAMT;
                ViewBag.Referenceid = REFERENCEID;
                ViewBag.Remarks = REMARKS;
                ViewBag.Particulars = PARTICULARS;
                ViewBag.Token = convertedtoken;
                ViewBag.validateToken = validateToken;

                return View("TokenView");
            }
            else
            {
                return View("Index");
            }
        }

        
        //Validating and getting txndetail request to api
        [HttpPost]
        public async Task<IActionResult> Request()
        {
            string username = "MER-1043-APP-1";
            string password = "Abcd@123";

            //validate txndetail api
            string apiUrl = "https://uat.connectips.com/connectipswebws/api/creditor/validatetxn";

            
            //get txndetail api
            string apiUrl1 = "https://uat.connectips.com/connectipswebws/api/creditor/gettxndetail";
           

            string vTxnId = TempData["validateTxnid"].ToString();
            string vTxnAmt = TempData["validateTxnAmt"].ToString();
            string vToken = TempData["validateToken"].ToString();

            string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            
    
          

            string requestBody = "{\"merchantId\": 1043, \"appId\": \"MER-1043-APP-1\", \"referenceId\": \"" + vTxnId+ "\", \"txnAmt\": " + vTxnAmt+ ", \"token\": \"" + vToken+ "\"}";

            Console.WriteLine("Request Body: " + requestBody);

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            Console.WriteLine("Content: " + await content.ReadAsStringAsync());


            //validation of transaction
            var txnvalResponse = await _httpClient.PostAsync(apiUrl, content);

            //Transaction Details
            var txndetailResponse = await _httpClient.PostAsync(apiUrl1, content);








            if (txnvalResponse.IsSuccessStatusCode)
            {
               
                string txnvalresponseContent = await txnvalResponse.Content.ReadAsStringAsync();
                var txnvalresponseObject = JsonSerializer.Deserialize<dynamic>(txnvalresponseContent);
               ViewBag.validationResponse = txnvalresponseObject;
             

                string txndetailresponseContent = await txndetailResponse.Content.ReadAsStringAsync();
                var txndetailresponseObject = JsonSerializer.Deserialize<dynamic>(txndetailresponseContent);
               
                ViewBag.txndetailResponse = txndetailresponseObject;
               


                return View("Details");
            }
            else
            {
                Console.WriteLine(StatusCode((int)txnvalResponse.StatusCode));
                return View("Error");

                
            }


        }

      

    //SuccessURL
        
        public IActionResult successpayment()
        {

            
            return View();
           ;

           
        }

       

        


        //FailureURl
        public IActionResult failedpayment()
        {
            return View();
        }

        public IActionResult Details()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
