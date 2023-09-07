using Newtonsoft.Json.Linq;
using Pursuit.Model;
using OfficeOpenXml;
using System.Text.RegularExpressions;
using Pursuit.Context;
using Pursuit.API.Controllers.v1;
/* =========================================================
Item Name: Read data from Excel File -ReadFromExcel
Author: Ortusolis for EvolveAccess Team
Version: 1.0
Copyright 2022 - 2023 - Evolve Access
============================================================ */
namespace Pursuit.Utilities
{
    public abstract class ReadFromExcel
    {
        private static int uploadedCount = 0;
        private static int errCount = 0;
        //private static IPursuitRepository<User> _userRepository;
        private static ILogger<ReadFromExcel> _logger;

        public ReadFromExcel(IPursuitRepository<User> userRepository, ILogger<ReadFromExcel> logger)
        {
            //_userRepository = userRepository;
            _logger = logger;
        }

        public static List<BulkUser> readCSVFromStream(byte[] streamedContent,
            IPursuitRepository<User> _userRepository)
        {
            MemoryStream mS = new MemoryStream();

            mS.Write(streamedContent, 0, streamedContent.Length);
            mS.Position = 0;

            List<BulkUser> fileData = new List<BulkUser>();


            StreamReader sr = new StreamReader(mS);

            while (!sr.EndOfStream)
            {
                BulkUser recData;
                var oneLine = sr?.ReadLine()?.Split(',');
                ICollection<JObject> Errors = new List<JObject>();
               
                    // Row by row...

                    Errors = new List<JObject>();
                    recData = new BulkUser();
                    recData.Err = false;
                string sl=oneLine[0].ToString();
                
                if (oneLine?.Length <1 ||sl=="")
                {
                    return fileData=null;
                }
                if (oneLine?.Length >=4)
                {
                    try
                    {
                        /* Serial Number */
                        try
                        {
                            recData.SlNo = int.Parse(oneLine[0].ToString());
                        }
                        catch (Exception ex)
                        {
                            recData.Err = true;

                            Errors.Add(addErrors(true, "SlNo", ex.Message));
                        }
                        /* FirstName */
                        try
                        {
                            recData.FirstName = oneLine[1].ToString();
                            if (recData.FirstName == null || recData.FirstName == "")
                            {
                                recData.Err = true;
                                Errors.Add(addErrors(true, "FirstName", "First Name Is Required"));

                            }
                            else
                            {
                                if (recData.FirstName.Length < 2 || recData.FirstName.Length > 50)
                                {
                                    recData.Err = true;
                                    Errors.Add(addErrors(true, "FirstName", "Invalid First Name"));

                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            recData.Err = true;
                            Errors.Add(addErrors(true, "FirstName", ex.Message));
                        }

                        /* LastName */
                        try
                        {
                            recData.LastName = oneLine[2].ToString();
                            if (recData.LastName == null || recData.LastName == "")
                            {
                                recData.Err = true;
                                Errors.Add(addErrors(true, "LastName", "Last Name Is Required"));

                            }
                            else
                            {
                                if (recData.LastName.Length < 1 || recData.LastName.Length > 50)
                                {
                                    recData.Err = true;
                                    Errors.Add(addErrors(true, "LastName", "Invalid Last Name"));

                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            recData.Err = true;
                            Errors.Add(addErrors(true, "LastName", ex.Message));
                        }

                        /* Email Id */
                        try
                        {
                            recData.Email = oneLine[3].ToString();
                            bool isEmailValid = Regex.IsMatch(recData.Email, Validations.EmailPattern, RegexOptions.IgnoreCase);
                            if (!isEmailValid)
                            {
                                recData.Err = true;
                                Errors.Add(addErrors(true, "Email", "Invalid Email ID"));
                            }
                            else
                            {
                                if ((_userRepository.FilterBy(x => x.Email == recData.Email)?.Count() ?? 0) > 0)
                                {
                                    recData.Err = true;
                                    Errors.Add(addErrors(true, "Email", "Email ID - User Already Present"));
                                }
                                else
                                {
                                    if (recData.Email.Length < 4 || recData.Email.Length > 50)
                                    {
                                        recData.Err = true;
                                        Errors.Add(addErrors(true, "Email", "Invalid Email ID"));

                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            recData.Err = true;
                            Errors.Add(addErrors(true, "Email", ex.Message));
                        }
                        if (oneLine?.Length != 4)
                        {
                            /* Phone */
                            try
                            {
                                if (oneLine[4] == null)
                                { oneLine[4] = ""; }
                                recData.Phone = oneLine[4].ToString();

                                if (recData.Phone != null && recData.Phone != "")
                                {
                                    bool isPhoneValid = Regex.IsMatch(recData.Phone, Validations.PhonePatterns);
                                    if (!isPhoneValid)
                                    {
                                        recData.Err = true;
                                        Errors.Add(addErrors(true, "Phone", "Invalid Phone Number"));
                                    }
                                    else
                                    {
                                        if ((_userRepository.FilterBy(x => x.Phone == recData.Phone && x.Phone != null && x.Phone != "")?.Count() ?? 0) > 0)
                                        {

                                            recData.Err = true;
                                            Errors.Add(addErrors(true, "Phone", "Phone Number - User Already Present"));
                                        }
                                    }

                                }

                            }
                            catch (Exception ex)
                            {
                                recData.Err = true;
                                Errors.Add(addErrors(true, "Phone", ex.Message));
                            }
                        }
                        else
                        {
                            recData.Phone = "";
                        }
                        var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(Errors);
                        recData.ErrDescription = jsonString;

                        uploadedCount++;
                    }
                    catch (Exception e)
                    {
                        recData.Err = true;
                        recData.ErrDescription = e.Message;
                        errCount++;
                    }
                }
                else
                {
                    
                    recData.Err = true;
                   
                    Errors.Add(addErrors(true, "Data", "Data field counts do not match"));
                    errCount++;
                }
                    fileData.Add(recData);

            }

            return fileData;
        }
        public static List<BulkUser> readExcelFromStream(byte[] streamedContent, 
            IPursuitRepository<User> _userRepository)
        {

            MemoryStream mS = new MemoryStream();

            mS.Write(streamedContent, 0, streamedContent.Length);

            #region Excel File Processing
            //**
            //This section takes the multipart file from request and process
            //Then send the details as response. 
            //Author: Sharath GT
            //**/
            //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            List<BulkUser> fileData = new List<BulkUser>();
            //using (var package = new ExcelPackage(new FileInfo(untrustedFileNameForStorage)))

            using (var package = new ExcelPackage(mS))
            {
                var firstSheet = package.Workbook.Worksheets["Sheet1"];
                if (firstSheet == null)
                    firstSheet = package.Workbook.Worksheets[0];
                var start = firstSheet.Dimension.Start;
                var end = firstSheet.Dimension.End;
                BulkUser recData;

                ICollection<JObject> Errors = new List<JObject>();
                for (int row = start.Row + 1; row <= end.Row; row++)
                { 
                    // Row by row...

                    Errors = new List<JObject>();
                    recData = new BulkUser();
                    recData.Err = false;
                    try
                    {
                        /* Serial Number */
                        try
                        {
                            
                            recData.SlNo = int.Parse(firstSheet.Cells[row, 1].Text);
                           
                        }
                        catch (Exception ex)
                        {
                            recData.Err = true;

                            Errors.Add(addErrors(true, "SlNo", ex.Message));
                        }
                        /* FirstName */
                        try
                        {
                            recData.FirstName = firstSheet.Cells[row, 2].Text;
                            if (recData.FirstName == null || recData.FirstName == "")
                            {
                                recData.Err = true;
                                Errors.Add(addErrors(true, "FirstName", "First Name Is Required"));

                            }
                            else
                            {
                                if (recData.FirstName.Length < 2 || recData.FirstName.Length > 50)
                                {
                                    recData.Err = true;
                                    Errors.Add(addErrors(true, "FirstName", "Invalid First Name"));

                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            recData.Err = true;
                            Errors.Add(addErrors(true, "FirstName", ex.Message));
                        }

                        /* LastName */
                        try
                        {
                            recData.LastName = firstSheet.Cells[row, 3].Text;
                            if (recData.LastName == null || recData.LastName == "")
                            {
                                recData.Err = true;
                                Errors.Add(addErrors(true, "LastName", "Last Name Is Required"));

                            }
                            else
                            {
                                if (recData.LastName.Length < 1 || recData.LastName.Length > 50)
                                {
                                    recData.Err = true;
                                    Errors.Add(addErrors(true, "LastName", "Invalid Last Name"));

                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            recData.Err = true;
                            Errors.Add(addErrors(true, "LastName", ex.Message));
                        }

                        /* Email Id */
                        try
                        {
                            recData.Email = firstSheet.Cells[row, 4].Text;
                            bool isEmailValid = Regex.IsMatch(recData.Email, Validations.EmailPattern, RegexOptions.IgnoreCase);
                            if (!isEmailValid)
                            {
                                recData.Err = true;
                                Errors.Add(addErrors(true, "Email", "Invalid Email ID"));
                            }
                            else
                            {

                                if ((_userRepository.FilterBy(x => x.Email == recData.Email)?.Count() ?? 0) > 0)
                                {
                                    recData.Err = true;
                                    Errors.Add(addErrors(true, "Email", "Email ID - User Already Present"));
                                }
                                else
                                {
                                    if (recData.Email.Length < 4 || recData.Email.Length > 50)
                                    {
                                        recData.Err = true;
                                        Errors.Add(addErrors(true, "Email", "Invalid Email ID"));

                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            recData.Err = true;
                            Errors.Add(addErrors(true, "Email", ex.Message));
                        }

                        /* Phone */
                        try
                        {
                            if (firstSheet.Cells[row, 5] == null)
                            {
                                recData.Phone = "";
                            }
                            else
                            {
                                recData.Phone = firstSheet.Cells[row, 5].Text;
                            }
                           
                           
                            if (recData.Phone != null && recData.Phone != "")
                            {

                                bool isPhoneValid = Regex.IsMatch(recData.Phone, Validations.PhonePatterns);
                                if (!isPhoneValid)
                                {
                                    recData.Err = true;
                                    Errors.Add(addErrors(true, "Phone", "Invalid Phone Number"));
                                }
                                if ((_userRepository.FilterBy(x => x.Phone == recData.Phone && x.Phone != null && x.Phone != "")?.Count() ?? 0) > 0)
                                {
                                    recData.Err = true;
                                    Errors.Add(addErrors(true, "Phone", "Phone Number- User Already Present"));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            recData.Err = true;
                            Errors.Add(addErrors(true, "Phone", ex.Message));
                        }
                        var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(Errors);
                        recData.ErrDescription = jsonString;

                        uploadedCount++;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Final catch");
                        recData.Err = true;
                        recData.ErrDescription = e.Message;
                        errCount++;
                    }
                  

                    fileData.Add(recData);
                }
                //var secondSheet = package.Workbook.Worksheets["Sheet2"];
            }
            #endregion

            return fileData;
        }

        private static JObject addErrors(bool err, string errField, string error)
        {
            dynamic colError = new JObject();
            colError.Err = err;
            colError.ErrField = errField;
            colError.ErrDescription = error;

            return colError;
        }
    }
}
