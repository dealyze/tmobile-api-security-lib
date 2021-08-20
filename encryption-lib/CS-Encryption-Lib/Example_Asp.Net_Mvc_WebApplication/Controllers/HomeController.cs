﻿using com.tmobile.oss.security.taap.jwe;
using Example_Asp.Net_Mvc_WebApplication.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Example_Asp.Net_Mvc_WebApplication.Controllers
{
    public class HomeController : Controller
    {
        private readonly Encryption encryption;

        public HomeController(Encryption encryption)
        {
            this.encryption = encryption;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var encryptedJweViewModel = new EncryptedJweViewModel()
            {
                PhoneNumber = "(555) 555-5555",
                ErrorMessage = " ",
                EncryptedJwe = string.Empty
            };

            return View(encryptedJweViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Index(string submitButton, string encryptedJwe, EncryptedJweViewModel encryptedJweViewModel)
        {
            try
            {
                if (submitButton == "Encrypt")
                {
                    encryptedJweViewModel.EncryptedJwe = await this.encryption.EncryptAsync(encryptedJweViewModel.PhoneNumber);
                }
                else if (submitButton == "Decrypt")
                {
                    encryptedJweViewModel.DecryptedPhoneNumber = await this.encryption.DecryptAsync(encryptedJwe);
                }
            }
            catch(Exception ex)
            {
                encryptedJweViewModel.DecryptedPhoneNumber = string.Empty;
                encryptedJweViewModel.ErrorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    encryptedJweViewModel.ErrorMessage += " " + ex.InnerException.Message;
                }
            }

            return View(encryptedJweViewModel);
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
