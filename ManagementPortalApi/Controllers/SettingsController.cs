﻿using ManagementPortalApi.Context;
using ManagementPortalApi.Extensions;
using ManagementPortalApi.Models.Authentication;
using ManagementPortalApi.Models.Settings;
using ManagementPortalApi.RateLimiting;
using ManagementPortalApi.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Specialized;
using System.Data;
using System.DirectoryServices;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Web;

namespace ManagementPortalApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Route("api/{controller}/{action}/{id:int?}")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly DataAccessLayer _DAL;
        private readonly SendEmail _sendemail;
        private readonly ILogger<SettingsController> _logger;
        private readonly DataEncryptor _dataencryptor;
        private readonly RandomStringGenerator _randomstringgenerator;

        public SettingsController(DataAccessLayer DAL, ILogger<SettingsController> logger, SendEmail sendemail, DataEncryptor dataencryptor, RandomStringGenerator randomstringgenerator)
        {
            _DAL = DAL;
            _logger = logger;
            _sendemail = sendemail;
            _dataencryptor = dataencryptor;
            _randomstringgenerator = randomstringgenerator;
        }

        #region Activity Log
        public void SystemActivityLog(int? ActivityID, string? ActivityDetails)
        {
            bool Result = false;

            ClaimsPrincipal claimsPrincipal = HttpContext.User;
            string HostName = Dns.GetHostName();
            IPHostEntry HostIPs = Dns.GetHostEntry(HostName);
            string IPAddress = HostIPs.AddressList[0].ToString();
            string UserID = (from c in claimsPrincipal.Claims where c.Type == "UserID" select c.Value).FirstOrDefault();
            var routeData = HttpContext.Request.RouteValues;
            string controllerName = routeData["controller"].ToString();
            string actionName = routeData["action"].ToString();
            string FormName = controllerName + "/" + actionName;
            string ActivityDetailsComplete = IPAddress + " " + ActivityDetails + " " + FormName;

            try
            {
                NameValueCollection nv = new NameValueCollection();
                nv.Clear();
                nv.Add("FormID-INT", "0");
                nv.Add("ActivityID-INT", ActivityID.ToString());
                nv.Add("UserID-INT", UserID);
                nv.Add("ActivityDetails-VARCHAR", ActivityDetailsComplete);
                Result = _DAL.InsertData("sp_insert_activitylog", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                _logger.LogInformation("{0} {1} {2}", controllerName, MethodBase.GetCurrentMethod().Name, ActivityDetailsComplete);
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2} {3}", controllerName, MethodBase.GetCurrentMethod().Name, ActivityDetailsComplete, ex.Message);
            }
        }
        #endregion

        #region RolesMaster
        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetRoles()
        {
            DataTable dt = new DataTable();
            try
            {
                dt = _DAL.GetData("sp_select_roles", null, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_roles");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_roles");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }
            return Ok(dt);
        }

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetRolesByID(string RoleID)
        {
            DataTable dt = new DataTable();
            string methodName = MethodBase.GetCurrentMethod().Name;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("RoleID-INT", RoleID);
                dt = _DAL.GetData("sp_select_roles_byid", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_roles_byid");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_roles_byid");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }
            return Ok(dt);
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult EditRoles([FromBody] Role role)
        {
            bool Result = false;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("RoleID-INT", role.RoleID);
                nv.Add("RoleName-VARCHAR", HttpUtility.HtmlEncode(role.RoleName));
                nv.Add("IsActive-BIT", role.IsActive == null ? "0" : "1");
                nv.Add("UserID-INT", role.UserID);
                Result = _DAL.InsertData("sp_update_role", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Update, ActivityLog.ActivityDetails_Update + "sp_update_role");

                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Update, ActivityLog.ActivityDetails_Update2 + "sp_update_role");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            if (Result)
            {
                return Ok(Result);
            }
            else
            {
                return BadRequest(Result);
            }
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult SaveRoles([FromBody] Role role)
        {
            bool Result = false;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("RoleName-VARCHAR", HttpUtility.HtmlEncode(role.RoleName));
                nv.Add("IsActive-BIT", role.IsActive == null ? "0" : "1");
                nv.Add("UserID-INT", role.UserID);
                Result = _DAL.InsertData("sp_insert_role", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert + "sp_insert_role");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert2 + "sp_insert_role");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            if (Result)
            {
                return Ok(Result);
            }
            else
            {
                return BadRequest(Result);
            }
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult DeleteRoles([FromBody] DeleteFromDB deleteFromDB_)
        {
            bool Result = false;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("RoleID-INT", deleteFromDB_.RoleID);
                nv.Add("UserID-INT", deleteFromDB_.UserID);
                Result = _DAL.InsertData("sp_delete_role", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Delete, ActivityLog.ActivityDetails_Delete + "sp_delete_role");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Delete, ActivityLog.ActivityDetails_Delete2 + "sp_delete_role");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            return Ok(Result);
        }
        #endregion

        #region UserMaster

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetUserDetail(string UserID)
        {
            DataTable dt = new DataTable();
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("UserID-INT", UserID);
                dt = _DAL.GetData("sp_getuserdetail", nv, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_getuserdetail");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_getuserdetail");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            return Ok(dt);

        }


        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetUserType(string TypeID)
        {
            DataTable dt = new DataTable();
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("TypeID-INT", TypeID);
                dt = _DAL.GetData("sp_select_usertype", nv, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_usertype");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_usertype");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            return Ok(dt);

        }

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetUsersByType(string TypeID, string UserID)
        {
            DataTable dt = new DataTable();
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("TypeID-INT", TypeID);
                nv.Add("UserID-INT", UserID);
                dt = _DAL.GetData("sp_select_users", nv, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_users");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_users");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " sp_select_users" + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }
            return Ok(dt);
        }

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetForms()
        {
            DataTable dt = new DataTable();
            try
            {
                dt = _DAL.GetData("sp_select_forms", null, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_forms");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_forms");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                BadRequest(ex.Message);
            }
            return Ok(dt);
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult EditUsers([FromBody] User user)
        {
            bool Result = false;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("UserID-INT", user.UserIDS);
                nv.Add("UserName-VARCHAR", HttpUtility.HtmlEncode(user.UserName));
                nv.Add("UserEmail-VARCHAR", HttpUtility.HtmlEncode(user.UserEmail));
                nv.Add("RoleID-INT", user.RoleID);
                nv.Add("DefaultFormID-INT", user.FormID);
                //nv.Add("UserTypeID-INT", user.UserTypeID);
                nv.Add("MobileNumber-VARCHAR", HttpUtility.HtmlEncode(user.MobileNumber));
                nv.Add("IsActive-BIT", user.IsActive == null ? "0" : "1");
                nv.Add("EditUserID-INT", user.EditUserID);
                nv.Add("IsAdUser-BIT", user.IsAdUser == null ? "0" : "1");
                nv.Add("Designation-VARCHAR", user.Designation == null ? "NULL" : HttpUtility.HtmlEncode(user.Designation));
                nv.Add("Department-VARCHAR", user.Department == null ? "NULL" : HttpUtility.HtmlEncode(user.Department));
                nv.Add("DomainName-VARCHAR", user.DomainName == null ? "NULL" : HttpUtility.HtmlEncode(user.DomainName));
                nv.Add("LoginName-VARCHAR", HttpUtility.HtmlEncode(user.LoginName));
                Result = _DAL.InsertData("sp_update_user", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Update, ActivityLog.ActivityDetails_Update + "sp_update_user");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Update, ActivityLog.ActivityDetails_Update2 + "sp_update_user");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            if (Result)
            {
                return Ok(Result);
            }
            else
            {
                return BadRequest(Result);
            }
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult SaveUsers([FromBody] User user)
        {
            bool Result = false;
            DataTable dt;
            try
            {

                string encrypted_password = string.Empty;
                string isAdUser = user.IsAdUser == null ? "0" : "1";
                if (isAdUser == "0")
                {
                    string RandomStrings = _randomstringgenerator.GetRandomString();
                    string EmailBody = ActivityLog.EmailBody + " RandomPassword : " + RandomStrings;
                    bool SendMail = _sendemail.SendEmailToUsers(new List<string> { user.UserEmail }, EmailBody, ActivityLog.EmailSubject, _DAL);

                    if (SendMail)
                    {
                        encrypted_password = _dataencryptor.HashPassword(RandomStrings);
                    }
                }

                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("UserID-INT", (user.UserIDS == null ? "NULL" : user.UserIDS));
                nv.Add("UserName-VARCHAR", HttpUtility.HtmlEncode(user.UserName));
                nv.Add("UserEmail-VARCHAR", (user.UserEmail == null ? "NULL" : HttpUtility.HtmlEncode(user.UserEmail)));
                nv.Add("UserPassword-VARCHAR", encrypted_password);
                nv.Add("RoleID-INT", user.RoleID);
                nv.Add("DefaultFormID-INT", user.FormID);
                //nv.Add("UserTypeID-INT", user.UserTypeID);
                nv.Add("MobileNumber-VARCHAR", HttpUtility.HtmlEncode(user.MobileNumber));
                nv.Add("IsActive-INT", user.IsActive == null ? "0" : "1");
                nv.Add("CreateUserID-INT", user.EditUserID);
                nv.Add("IsAdUser-BIT", user.IsAdUser == null ? "0" : "1");
                nv.Add("Designation-VARCHAR", user.Designation == null ? "NULL" : HttpUtility.HtmlEncode(user.Designation));
                nv.Add("Department-VARCHAR", user.Department == null ? "NULL" : HttpUtility.HtmlEncode(user.Department));
                nv.Add("DomainName-VARCHAR", user.DomainName == null ? "NULL" : HttpUtility.HtmlEncode(user.DomainName));
                nv.Add("LoginName-VARCHAR", HttpUtility.HtmlEncode(user.LoginName));
                dt = _DAL.GetData("sp_insert_user", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert + "sp_insert_user");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert2 + "sp_insert_user");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            if (dt != null)
            {
                return Ok(dt);
            }
            else
            {
                return BadRequest(dt);
            }
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult DeleteUsers([FromBody] DeleteFromDB deleteFromDB_)
        {
            bool Result = false;

            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("UserID-INT", deleteFromDB_.UserID);
                nv.Add("EditUserID-INT", deleteFromDB_.EditUserID);
                Result = _DAL.InsertData("sp_delete_user", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Delete, ActivityLog.ActivityDetails_Delete + "sp_delete_user");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Delete, ActivityLog.ActivityDetails_Delete2 + "sp_delete_user");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            return Ok(Result);
        }


        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetFeedFormat()
        {
            DataTable dt = new DataTable();
            try
            {
                dt = _DAL.GetData("sp_select_feed_format", null, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_feed_format");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_feed_format");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                BadRequest(ex.Message);
            }
            return Ok(dt);
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult SaveFeedUrlFormt([FromBody] FeedFormatURL feedFormatURL)
        {
            bool Result = false;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("FormatID-INT", HttpUtility.HtmlEncode(feedFormatURL.FormatID));
                nv.Add("Feed_URL-NVARCHAR", HttpUtility.HtmlEncode(feedFormatURL.Feed_URL));
                nv.Add("CreatedBy-INT", feedFormatURL.CreatedBy);
                nv.Add("UserID-INT", feedFormatURL.UserID);
                Result = _DAL.InsertData("sp_InsertFeedURLplusFormat", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert + "sp_InsertFeedURLplusFormat");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert2 + "sp_InsertFeedURLplusFormat");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            if (Result)
            {
                return Ok(Result);
            }
            else
            {
                return BadRequest(Result);
            }
        }

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetFeedFormatURL()
        {
            DataTable dt = new DataTable();
            try
            {
                dt = _DAL.GetData("sp_select_feedurlformat", null, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_feedurlformat");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_feedurlformat");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }
            return Ok(dt);
        }

        #endregion

        #region RolesMapping

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetMapping(string RoleId)
        {
            DataTable dt = new DataTable();
            try
            {
                NameValueCollection? nv = new NameValueCollection();

                nv.Clear();
                nv.Add("RoleID-INT", RoleId);
                dt = _DAL.GetData("sp_select_rolesmapping", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_rolesmapping");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_rolesmapping");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }
            return Ok(dt);
        }

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetMapping_2(string RoleId)
        {
            DataTable dt = new DataTable();
            try
            {
                NameValueCollection? nv = new NameValueCollection();

                nv.Clear();
                nv.Add("RoleID-INT", RoleId);
                dt = _DAL.GetData("sp_select_rolesmapping_2", nv, _DAL.CSManagementPortalDatabase);
                nv = null;
                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_rolesmapping_2");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_rolesmapping_2");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }
            return Ok(dt);
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult UpdateRolesMapping([FromBody] RoleMapping mapping)
        {
            bool Result = false;
            bool Result2 = false;
            try
            {
                NameValueCollection? nv = new NameValueCollection();

                nv.Clear();
                nv.Add("RoleID-INT", mapping.RoleID);
                Result = _DAL.InsertData("sp_delete_rolemapping", nv, _DAL.CSManagementPortalDatabase);

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Delete, ActivityLog.ActivityDetails_Delete + "sp_delete_rolemapping");

                    foreach (RoleMappingCollection item in mapping.RoleMappingCollection)
                    {
                        nv.Clear();
                        nv.Add("RoleID-INT", mapping.RoleID);
                        nv.Add("UserID-INT", mapping.UserID);
                        nv.Add("FormsID-INT", item.FormsID);
                        nv.Add("AllowInsert-BIT", item.AllowInsert);
                        nv.Add("AllowUpdate-BIT", item.AllowUpdate);
                        nv.Add("AllowDelete-BIT", item.AllowDelete);
                        nv.Add("IsNavView-BIT", item.IsNavView);
                        Result2 = _DAL.InsertData("sp_update_rolesmapping", nv, _DAL.CSManagementPortalDatabase);

                        if (Result2)
                        {
                            SystemActivityLog(ActivityLog.ActivityID_Update, ActivityLog.ActivityDetails_Update + "sp_update_rolesmapping");
                        }
                        else
                        {
                            SystemActivityLog(ActivityLog.ActivityID_Update, ActivityLog.ActivityDetails_Update2 + "sp_update_rolesmapping");
                        }
                    }
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Delete, ActivityLog.ActivityDetails_Delete2 + "sp_delete_rolemapping");
                }
                nv = null;
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            if (Result)
            {
                return Ok(Result);
            }
            else
            {
                return BadRequest(Result);
            }
        }

        #endregion

        #region API Acess
        [HttpGet]
        public IActionResult GetAllowedApiByRole(string RoleID)
        {
            DataTable dt = new DataTable();
            string methodName = MethodBase.GetCurrentMethod().Name;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("RoleID-INT", RoleID);
                dt = _DAL.GetData("sp_select_roles_byid", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_roles_byid");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_roles_byid");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " sp_select_roles_byid" + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }
            return Ok(dt);
        }
        #endregion

        #region AdUsers

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult FetchAdUsers()
        {
            DataTable dt = new DataTable();
            DataTable dtUsers = new DataTable();
            try
            {

                dt = _DAL.GetData("sp_select_ldapsettings2", null, _DAL.CSManagementPortalDatabase);
                string Ldap = dt.Rows[0]["LdapPath"].ToString();
                string userid = dt.Rows[0]["LdapUser"].ToString();
                string password = dt.Rows[0]["LdapPassword"].ToString();

                dtUsers = LoadAdUsers(Ldap, userid, password);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_ldapsettings2");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_ldapsettings2");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest(ex.Message);
            }

            return Ok(dtUsers);

        }

        private DataTable LoadAdUsers(string Ldap, string userid, string password)
        {

            DirectoryEntry myLdapConnection = new DirectoryEntry(Ldap, userid, _dataencryptor.DecryptPassword(password));
            DirectorySearcher search = new DirectorySearcher(myLdapConnection) { Filter = ("(objectClass=user)") };
            search.CacheResults = true;

            SearchResultCollection allResults = search.FindAll();
            string varStr = myLdapConnection.Name.ToString();
            string[] varDomain = varStr.Split('=');

            DataTable table = new DataTable();
            table.TableName = "AdUsers";
            table.Columns.Add("UserName");
            table.Columns.Add("UserID");
            table.Columns.Add("EmailID");
            table.Columns.Add("Designation");
            table.Columns.Add("Department");
            table.Columns.Add("TelephoneNo");
            table.Columns.Add("DomainName");
            foreach (SearchResult searchResult in allResults)
            {
                string UserName = "";
                string UserID = "";
                string EmailID = "";
                string Department = "";
                string Designation = "";
                string TelephoneNo = "";
                string DomainName = varDomain[1];
                if (searchResult.Properties["name"] != null && searchResult.Properties["name"].Count > 0)
                {
                    UserName = searchResult.Properties["name"][0].ToString();
                }
                if (UserName == "MettisDev2")
                {

                }
                if (searchResult.Properties["department"] != null && searchResult.Properties["department"].Count > 0)
                {
                    Department = searchResult.Properties["department"][0].ToString();
                }
                if (searchResult.Properties["title"] != null && searchResult.Properties["title"].Count > 0)
                {
                    Designation = searchResult.Properties["title"][0].ToString();
                }
                if (searchResult.Properties["samAccountName"] != null && searchResult.Properties["samAccountName"].Count > 0)
                {
                    UserID = searchResult.Properties["samAccountName"][0].ToString();
                }
                if (searchResult.Properties["mail"] != null && searchResult.Properties["mail"].Count > 0)
                {
                    EmailID = searchResult.Properties["mail"][0].ToString();
                }
                if (searchResult.Properties["telephonenumber"] != null && searchResult.Properties["telephonenumber"].Count > 0)
                {
                    TelephoneNo = searchResult.Properties["telephonenumber"][0].ToString();
                }

                DataRow NewRow = table.NewRow();
                NewRow["UserName"] = UserName; ;
                NewRow["UserID"] = UserID;
                NewRow["EmailID"] = EmailID;
                NewRow["Designation"] = Designation;
                NewRow["Department"] = Department;
                NewRow["TelephoneNo"] = TelephoneNo;
                NewRow["DomainName"] = DomainName;
                table.Rows.Add(NewRow);
            }
            return table;
        }
        #endregion

        #region SmtpSetting

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult SmtpSettingUpdate([FromBody] SMTPSettings smtpSetting)
        {

            bool Result = false;

            DataTable dt;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("Smtp-VARCHAR", HttpUtility.HtmlEncode(smtpSetting.Smtp));
                nv.Add("SenderEmailID-VARCHAR", HttpUtility.HtmlEncode(smtpSetting.SenderEmailID));
                nv.Add("SmtpPassword-VARCHAR", smtpSetting.SmtpPassword);
                nv.Add("SmtpPort-VARCHAR", HttpUtility.HtmlEncode(smtpSetting.SmtpPort));
                nv.Add("DisplayName-VARCHAR", HttpUtility.HtmlEncode(smtpSetting.DisplayName));
                nv.Add("EnableSSL-BIT", smtpSetting.EnableSSL.ToString());

                Result = _DAL.InsertData("sp_insert_smtpsetting", nv, _DAL.CSManagementPortalDatabase);

                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert + "sp_insert_smtpsetting");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert2 + "sp_insert_smtpsetting");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest(ex.Message);
            }

            return Ok(Result);


        }

        [HttpGet]
        public IActionResult SmtpSettingGet()
        {
            DataTable dt = new DataTable();
            try
            {
                dt = _DAL.GetData("sp_select_SmtpSettings", null, _DAL.CSManagementPortalDatabase);
                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_SmtpSettings");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_SmtpSettings");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest(ex.Message);
            }

            return Ok(dt);
        }
        #endregion


        #region News Master Tags
        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetTagType()
        {
            DataTable dt = new DataTable();
            try
            {
                dt = _DAL.GetData("sp_select_slugtype", null, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_slugtype");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_slugtype");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }
            return Ok(dt);
        }


        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetNewsTags()
        {
            DataTable dt = new DataTable();
            try
            {
                dt = _DAL.GetData("sp_select_mastertags", null, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_mastertags");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_mastertags");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }
            return Ok(dt);
        }

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetTagsByID(string TagID)
        {
            DataTable dt = new DataTable();
            string methodName = MethodBase.GetCurrentMethod().Name;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("TagID-INT", TagID);
                dt = _DAL.GetData("sp_get_tagsdetail", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_get_tagsdetail");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_get_tagsdetail");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }
            return Ok(dt);
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult EditTags([FromBody] TagsMaster tagsMaster)
        {
            bool Result = false;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("TagID-INT", tagsMaster.TagID);
                nv.Add("TagName-NVARCHAR", HttpUtility.HtmlEncode(tagsMaster.TagName));
                nv.Add("TypeID-INT", HttpUtility.HtmlEncode(tagsMaster.TypeID));
                Result = _DAL.InsertData("sp_update_mastertag", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Update, ActivityLog.ActivityDetails_Update + "sp_update_mastertag");

                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Update, ActivityLog.ActivityDetails_Update2 + "sp_update_mastertag");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            if (Result)
            {
                return Ok(Result);
            }
            else
            {
                return BadRequest(Result);
            }
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult SaveNewsTags([FromBody] TagsMaster tagsMaster)
        {
            bool Result = false;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("TagName-NVARCHAR", HttpUtility.HtmlEncode(tagsMaster.TagName));
                nv.Add("TypeID-INT", HttpUtility.HtmlEncode(tagsMaster.TypeID));
                nv.Add("UserID-INT", tagsMaster.UserID);
                Result = _DAL.InsertData("sp_insert_MasterTags", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert + "sp_insert_MasterTags");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert2 + "sp_insert_MasterTags");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            if (Result)
            {
                return Ok(Result);
            }
            else
            {
                return BadRequest(Result);
            }
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult DeleteTags([FromBody] DeleteFromDB deleteFromDB_)
        {
            bool Result = false;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("TagID-INT", deleteFromDB_.TagID);
                Result = _DAL.InsertData("sp_delete_mastertags", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Delete, ActivityLog.ActivityDetails_Delete + "sp_delete_mastertags");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Delete, ActivityLog.ActivityDetails_Delete2 + "sp_delete_mastertags");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            return Ok(Result);
        }

        #endregion

        #region SlugsMaster
        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetSlugs()
        {
            DataTable dt = new DataTable();
            try
            {
                dt = _DAL.GetData("sp_selectAllSlugs", null, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_selectAllSlugs");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_selectAllSlugs");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }
            return Ok(dt);
        }

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetSlugsByID(string SlugID)
        {
            DataTable dt = new DataTable();
            string methodName = MethodBase.GetCurrentMethod().Name;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("SlugID-INT", SlugID);
                dt = _DAL.GetData("sp_get_AllSlugs", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_get_AllSlugs");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_get_AllSlugs");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }
            return Ok(dt);
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult EditSlugs([FromBody] SlugsMaster slugsmaster)
        {
            bool Result = false;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("SlugID-INT", slugsmaster.SlugID);
                nv.Add("SlugName-NVARCHAR", HttpUtility.HtmlEncode(slugsmaster.SlugName));
                nv.Add("TypeID-INT", HttpUtility.HtmlEncode(slugsmaster.TypeID));
                nv.Add("UserID-INT", slugsmaster.UserID);
                Result = _DAL.InsertData("sp_UpdateSlug", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Update, ActivityLog.ActivityDetails_Update + "sp_UpdateSlug");

                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Update, ActivityLog.ActivityDetails_Update2 + "sp_UpdateSlug");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            if (Result)
            {
                return Ok(Result);
            }
            else
            {
                return BadRequest(Result);
            }
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult SaveSlugs([FromBody] SlugsMaster slugsmaster)
        {
            bool Result = false;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("SlugName-NVARCHAR", HttpUtility.HtmlEncode(slugsmaster.SlugName));
                nv.Add("TypeID-INT", HttpUtility.HtmlEncode(slugsmaster.TypeID));
                nv.Add("UserID-INT", slugsmaster.UserID);
                Result = _DAL.InsertData("sp_insert_masterslugs", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert + "sp_insert_masterslugs");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert2 + "sp_insert_masterslugs");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            if (Result)
            {
                return Ok(Result);
            }
            else
            {
                return BadRequest(Result);
            }
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult DeleteSlugs([FromBody] DeleteFromDB deleteFromDB_)
        {
            bool Result = false;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("SlugID-INT", deleteFromDB_.SlugID);
                Result = _DAL.InsertData("sp_DeleteSlug", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Delete, ActivityLog.ActivityDetails_Delete + "sp_DeleteSlug");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Delete, ActivityLog.ActivityDetails_Delete2 + "sp_DeleteSlug");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            return Ok(Result);
        }

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetSlugType()
        {
            DataTable dt = new DataTable();
            try
            {
                dt = _DAL.GetData("sp_select_slugtype", null, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_slugtype");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_slugtype");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }
            return Ok(dt);
        }
        #endregion

        #region API ENDPOINT

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetApiEndPoint()
        {
            DataTable dt = new DataTable();
            try
            {
                dt = _DAL.GetData("sp_select_apiendpoint", null, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_apiendpoint");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_apiendpoint");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }
            return Ok(dt);
        }

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetApiEndPointByID(string ApiModuleID)
        {
            DataTable dt = new DataTable();
            string methodName = MethodBase.GetCurrentMethod().Name;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("ApiModuleID-INT", ApiModuleID);
                dt = _DAL.GetData("sp_get_apiendpoint", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_get_apiendpoint");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_get_apiendpoint");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }
            return Ok(dt);
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult EditApiEndPoint([FromBody] ApiEndPoint apiEndPoint)
        {
            bool Result = false;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("ApiModuleID-INT", apiEndPoint.ApiModuleID);
                nv.Add("ModuleName-NVARCHAR", HttpUtility.HtmlEncode(apiEndPoint.ModuleName));
                nv.Add("ApiDelay-INT", HttpUtility.HtmlEncode(apiEndPoint.ApiDelay));
                Result = _DAL.InsertData("sp_update_apiendpoint", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Update, ActivityLog.ActivityDetails_Update + "sp_update_apiendpoint");

                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Update, ActivityLog.ActivityDetails_Update2 + "sp_update_apiendpoint");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            if (Result)
            {
                return Ok(Result);
            }
            else
            {
                return BadRequest(Result);
            }
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult SaveApiEndPoint([FromBody] ApiEndPoint apiEndPoint)
        {
            bool Result = false;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("ModuleName-NVARCHAR", HttpUtility.HtmlEncode(apiEndPoint.ModuleName));
                nv.Add("ApiDelay-INT", HttpUtility.HtmlEncode(apiEndPoint.ApiDelay));
                Result = _DAL.InsertData("sp_save_apiendpoint", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert + "sp_save_apiendpoint");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert2 + "sp_save_apiendpoint");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            if (Result)
            {
                return Ok(Result);
            }
            else
            {
                return BadRequest(Result);
            }
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult DeleteApiEndPoint([FromBody] DeleteFromDB deleteFromDB_)
        {
            bool Result = false;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("ApiModuleID-INT", deleteFromDB_.ApiModuleID);
                Result = _DAL.InsertData("sp_delete_api_endpoint", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Delete, ActivityLog.ActivityDetails_Delete + "sp_delete_api_endpoint");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Delete, ActivityLog.ActivityDetails_Delete2 + "sp_delete_api_endpoint");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            return Ok(Result);
        }

        #endregion

        #region API UserMaster

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetApiUser()
        {
            DataTable dt = new DataTable();
            try
            {
                dt = _DAL.GetData("sp_select_apiuser", null, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_apiuser");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_apiuser");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }
            return Ok(dt);
        }

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetApiUserByID(string UserID)
        {
            DataTable dt = new DataTable();
            string methodName = MethodBase.GetCurrentMethod().Name;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("UserID-INT", UserID);
                dt = _DAL.GetData("sp_get_apiuser", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_get_apiuser");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_get_apiuser");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }
            return Ok(dt);
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult EditApiUser([FromBody] ApiUser apiUser)
        {
            bool Result = false;
            try
            {
                string encrypted_password = string.Empty;
                string RandomStrings = _randomstringgenerator.GetRandomString();
                encrypted_password = _dataencryptor.HashPassword(RandomStrings);

                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("UserID-INT", apiUser.UserID);
                nv.Add("UserName-NVARCHAR", HttpUtility.HtmlEncode(apiUser.UserName));
                nv.Add("UserEmail-NVARCHAR", HttpUtility.HtmlEncode(apiUser.UserEmail));
                nv.Add("UserPassword-NVARCHAR", HttpUtility.HtmlEncode(encrypted_password));
                nv.Add("AccessLevel-INT", HttpUtility.HtmlEncode(apiUser.AccessLevel));
                nv.Add("IsActive-BIT", HttpUtility.HtmlEncode(apiUser.IsActive));
                Result = _DAL.InsertData("sp_update_apiuser", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Update, ActivityLog.ActivityDetails_Update + "sp_update_apiuser");

                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Update, ActivityLog.ActivityDetails_Update2 + "sp_update_apiuser");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            if (Result)
            {
                return Ok(Result);
            }
            else
            {
                return BadRequest(Result);
            }
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult SaveApiUser([FromBody] ApiUser apiUser)
        {
            bool Result = false;
            try
            {
                string encrypted_password = string.Empty;
                string RandomStrings = _randomstringgenerator.GetRandomString();
                encrypted_password = _dataencryptor.HashPassword(apiUser.UserPassword);

                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("UserName-NVARCHAR", HttpUtility.HtmlEncode(apiUser.UserName));
                nv.Add("UserEmail-NVARCHAR", HttpUtility.HtmlEncode(apiUser.UserEmail));
                nv.Add("UserPassword-NVARCHAR", HttpUtility.HtmlEncode(encrypted_password));
                nv.Add("AccessLevel-INT", HttpUtility.HtmlEncode(apiUser.AccessLevel));
                nv.Add("IsActive-BIT", HttpUtility.HtmlEncode(apiUser.IsActive));
                Result = _DAL.InsertData("sp_save_apiuser", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert + "sp_save_apiuser");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert2 + "sp_save_apiuser");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            if (Result)
            {
                return Ok(Result);
            }
            else
            {
                return BadRequest(Result);
            }
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult DeleteApiUser([FromBody] DeleteFromDB deleteFromDB_)
        {
            bool Result = false;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("UserID-INT", deleteFromDB_.ApiUserID);
                Result = _DAL.InsertData("sp_delete_apiuser", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Delete, ActivityLog.ActivityDetails_Delete + "sp_delete_apiuser");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Delete, ActivityLog.ActivityDetails_Delete2 + "sp_delete_apiuser");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            return Ok(Result);
        }

        #endregion

    }
}
