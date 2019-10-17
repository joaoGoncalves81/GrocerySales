using ApplicationCore;
using ApplicationCore.DTOs;
using ApplicationCore.Entities;
using ApplicationCore.Interfaces;
using AutoMapper;
using Backoffice.Extensions;
using Backoffice.Interfaces;
using Infrastructure.Data;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Backoffice.Services
{
    public class BackofficeService : IBackofficeService
    {
        private readonly BackofficeSettings _settings;
        private readonly IInvoiceService _invoiceService;

        public BackofficeService(
            IOptions<BackofficeSettings> options,
            IInvoiceService invoiceService)
        {
            _settings = options.Value;
            _invoiceService = invoiceService;
        }
        public bool CheckIfFileExists(string fullpath, string fileName)
        {
            return System.IO.File.Exists(Path.Combine(fullpath, fileName));
        }

        public void DeleteFile(string fullpath)
        {
            if (System.IO.File.Exists(fullpath))
                System.IO.File.Delete(fullpath);
        }

        public void DeleteFile(string fullpath, string fileName)
        {
            if (System.IO.File.Exists(Path.Combine(fullpath, fileName)))
                System.IO.File.Delete(Path.Combine(fullpath, fileName));
        }

        public PictureInfo SaveFile(IFormFile formFile, string fullPath, string uriPath, string addToFilename, bool resize = false, int width = 0, int height = 0)
        {
            var info = new PictureInfo
            {
                Filename = formFile.GetFileNameSimplify(),
                Extension = formFile.GetExtension()
            };
            var filename = formFile.GetFileName();

            if (!string.IsNullOrEmpty(addToFilename))
            {
                var name = filename.Substring(0, filename.LastIndexOf('.')) + $"-{addToFilename}";
                filename = name + filename.Substring(filename.LastIndexOf('.'));
            }

            //High
            var fileNameHigh = filename.Replace(".", "-high.");
            var fileHighPath = Path.Combine(
                fullPath,
                fileNameHigh);
            using (var stream = new FileStream(fileHighPath, FileMode.Create))
            {
                formFile.CopyTo(stream);
            }

            var filePath = Path.Combine(
                fullPath,
                filename);

            //Medium
            if (resize)
            {
                using (Image<Rgba32> image = Image.Load(formFile.OpenReadStream()))
                {
                    image.Mutate(x => x
                         .Resize(width, height));

                    image.Save(filePath); // Automatic encoder selected based on extension.

                    var options = new ResizeOptions
                    {
                        Mode = ResizeMode.Crop,
                        Size = new SixLabors.Primitives.Size(width, height)
                    };

                    image.Mutate(x => x.Resize(options));      

                    image.Save(filePath, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 90 }); // Automatic encoder selected based on extension.
                }
            }
            else
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    formFile.CopyTo(stream);
                }

            }

            info.Location = filePath;
            info.PictureUri = uriPath + filename;
            info.PictureHighUri = uriPath + fileNameHigh;

            return info;
        }

        public async Task SaveFileAsync(byte[] bytes, string fullPath, string filename)
        {
            var filePath = Path.Combine(
                fullPath,
                filename);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await stream.WriteAsync(bytes, 0, bytes.Length);
            }

        }

        public async Task<byte[]> GetInvoicePDFAsync(SageApplicationType application, long invoiceId)
        {
            return await _invoiceService.GetPDFInvoiceAsync(application, invoiceId);
        }

        public async Task<byte[]> GetReceiptPDFAsync(long invoiceId, long paymentId)
        {
            return await _invoiceService.GetPDFReceiptAsync(SageApplicationType.DAMA_BACKOFFICE, invoiceId, paymentId);
        }

        public async Task<List<(string, byte[])>> GetOrderDocumentsAsync(int id)
        {
            var invoiceFileName = string.Format(_settings.InvoiceNameFormat, id);
            //var receiptFileName = string.Format(_settings.ReceiptNameFormat, id);
            var invoicePath = Path.Combine(_settings.InvoicesFolderFullPath, invoiceFileName);
            //var receiptPath = Path.Combine(_settings.InvoicesFolderFullPath, receiptFileName);
            List<(string Filename, byte[] Bytes)> files = new List<(string, byte[])>();
            if (File.Exists(invoicePath))
                files.Add((invoiceFileName, await File.ReadAllBytesAsync(invoicePath)));
            //if (File.Exists(receiptPath))
            //    files.Add((receiptFileName, await File.ReadAllBytesAsync(receiptPath)));

            return files;
        }
    }
}
