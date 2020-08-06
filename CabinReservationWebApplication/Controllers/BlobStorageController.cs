using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CabinReservationWebApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace CabinReservationWebApplication.Controllers
{
    public class BlobStorageController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly CloudStorageAccount _cloudStorageAccount;
        private readonly CloudBlobClient _blobClient;
        private readonly CloudBlobContainer _container;

        private readonly ServiceRepository _service;

        public BlobStorageController(IConfiguration configuration, ServiceRepository service)
        {
            _configuration = configuration;

            string blobStorageConnection = _configuration.GetConnectionString("BlobStorage");
            _cloudStorageAccount = CloudStorageAccount.Parse(blobStorageConnection);
            _blobClient = _cloudStorageAccount.CreateCloudBlobClient();
            _container = _blobClient.GetContainerReference("cabinreservationsystemblob");

            _service = service;
        }

        // Returns view where Administrator/CabinOwner can upload images to Cabin
        // BlobStorage example taken from https://tutexchange.com/uploading-download-and-delete-files-in-azure-blob-storage-using-asp-net-core-3-1/
        // Uploads images to Azure BlobStorage "cabinreservationsystemblob"
        [HttpGet]
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<IActionResult> Upload(int cabinId, bool errorMessage)
        {
            try
            {
                if(errorMessage) ViewBag.SelectImage = "Valitse ensin kuva";

                ViewBag.CabinImages = await _service.GetCabinImages(cabinId);
                ViewBag.CabinId = cabinId;
                return View();
            }
            catch 
            {
                return View("ErrorPage");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrator, CabinOwner")]
        // Uploads image to blob storage and posts CabinImage to DB
        public async Task<IActionResult> Upload(CabinImage cabinImage)
        {
            try
            {
                if(cabinImage.Files == null)
                {
                    return RedirectToAction("Upload", new { cabinId = cabinImage.CabinId, errorMessage = true });
                }

                var imageName = $"{Guid.NewGuid().ToString()}";

                cabinImage.ImageUrl = imageName;
                var postCabinImage = await _service.PostCabinImage(User, cabinImage);
                if (!postCabinImage) return View("ErrorPage");

                CloudBlockBlob blockBlob = _container.GetBlockBlobReference(imageName);
                await using (var data = cabinImage.Files.OpenReadStream())
                {
                    await blockBlob.UploadFromStreamAsync(data);
                }

                return RedirectToAction("Upload", new { cabinId = cabinImage.CabinId });
            }
            catch 
            {
                return View("ErrorPage");
            }
        }

        // Delete a blob from BlobStorage "cabinreservationsystemblob" and CabinReservations DB also
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<IActionResult> DeleteBlob(string blobName, int cabinImageId, int Id)
        {
            //string blobstorageconnection = _configuration.GetConnectionString("BlobStorage");
            //CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);
            //CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            //CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("cabinreservationsystemblob");
            //var blob = cloudBlobContainer.GetBlobReference(blobName);
            try
            {
                var blob = _container.GetBlobReference(blobName);
                
                var deleteBlob = await blob.DeleteIfExistsAsync();

                var deleteCabinImage = await _service.DeleteCabinImage(User, cabinImageId);

                if(deleteBlob && deleteCabinImage) return RedirectToAction("Upload", new { cabinId = Id });

                return View("ErrorPage");
            }
            catch 
            {
                return View("ErrorPage");
            }
        }

        //// Returns all blobs from BlobStorage "cabinreservationsystemblob"
        //public async Task<IActionResult> ShowAllBlobs()
        //{
        //    //string blobstorageconnection = _configuration.GetConnectionString("BlobStorage");
        //    //CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);
        //    //CloudBlobClient blobClient = cloudStorageAccount.CreateCloudBlobClient();
        //    //CloudBlobContainer container = blobClient.GetContainerReference("cabinreservationsystemblob");
        //    //CloudBlobDirectory dirb = _container.GetDirectoryReference("cabinreservationsystemblob");

        //    BlobResultSegment resultSegment = await _container.ListBlobsSegmentedAsync(string.Empty, true, BlobListingDetails.Metadata, 100, null, null, null);

        //    List<BlobFiles> fileList = new List<BlobFiles>();

        //    foreach (var blobItem in resultSegment.Results)
        //    {
        //        // A flat listing operation returns only blobs, not virtual directories.
        //        var blob = (CloudBlob)blobItem;
        //        fileList.Add(new BlobFiles()
        //        {
        //            FileName = blob.Name,
        //            FileSize = Math.Round((blob.Properties.Length / 1024f) / 1024f, 2).ToString(),
        //            ModifiedOn = DateTime.Parse(blob.Properties.LastModified.ToString()).ToLocalTime().ToString()
        //        });
        //    }

        //    return View(fileList);
        //}

        //// Downloads a blob from BlobStorage "cabinreservationsystemblob"
        //public async Task<IActionResult> DownloadBlob(string blobName)
        //{
        //    CloudBlockBlob blockBlob;
        //    await using (MemoryStream memoryStream = new MemoryStream())
        //    {
        //        //string blobstorageconnection = _configuration.GetConnectionString("BlobStorage");
        //        //CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);
        //        //CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
        //        //CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("cabinreservationsystemblob");
        //        //blockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);
        //        blockBlob = _container.GetBlockBlobReference(blobName);
        //        await blockBlob.DownloadToStreamAsync(memoryStream);
        //    }

        //    Stream blobStream = blockBlob.OpenReadAsync().Result;
        //    return File(blobStream, blockBlob.Properties.ContentType, blockBlob.Name);
        //}

    }
}