using DocManagementAPI.Controllers;
using DocManagementAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using Xunit;


namespace DocManagmentUnitTest
{
    public class DocumentManagementAPITesting
    {
        private readonly DbContextOptionsBuilder<DocumentContext> optionsBuilder;
        private readonly DocumentContext context;
        private readonly DocumentsController docController;
        
        public DocumentManagementAPITesting()
        {
            optionsBuilder = new DbContextOptionsBuilder<DocumentContext>();
            optionsBuilder.UseInMemoryDatabase(databaseName: "DocumentManagementDBTest");
            context = new DocumentContext(optionsBuilder.Options);
            docController = new DocumentsController(context);
            Document document = new Document();
            document.ID = Guid.NewGuid();
            document.Name = "Test File.pdf";
            document.Location = "/api/documents/Test File.pdf";
            document.Size =100; //storing in mb
            context.Documents.Add(document);
            context.SaveChanges();
           

        }

        [Fact]
        public void getDocumentTest()
        {
            var result = docController.Get().ToArray<dynamic>();            
            Assert.True(result[0].GetType().GetProperty("Name").GetValue(result[0], null) == "Test File.pdf");
            Assert.True(result[0].GetType().GetProperty("Location").GetValue(result[0], null) == "/api/documents/Test File.pdf");
            Assert.True(result[0].GetType().GetProperty("Size").GetValue(result[0], null) == 100);
        }
       
        [Fact]
        public void UploadTest()
        {
            Mock<IFormFile> fileMock = createMock("Nomination form.pdf", "pdf");
            var result = docController.OnPostUploadAsync(fileMock.Object).Result;
            Assert.True(((Microsoft.AspNetCore.Mvc.ObjectResult)result).Value.ToString() == "Uploaded");
          
        }
        [Fact]
        public void UploadNonPDFTest()
        {
           
            Mock<IFormFile> fileMock = createMock("sea.jpg", "jpg");
            var result = docController.OnPostUploadAsync(fileMock.Object).Result;
            Assert.True(((Microsoft.AspNetCore.Mvc.ObjectResult)result).Value.ToString() == "Not a valid file type");
        }
       [Fact]
        public void UploadLargerPDFTest()
        {
            Mock<IFormFile> fileMock = createMock("DesignPatterns.pdf","pdf");
            var result = docController.OnPostUploadAsync(fileMock.Object).Result;
            Assert.True(((Microsoft.AspNetCore.Mvc.ObjectResult)result).Value.ToString() == "File size is greater than 5 MB");
        }
       
        [Fact]
        public void DownloadFileTest()
        {
            UploadTest();
            var result = docController.DownloadFile("Nomination form.pdf");
            Assert.True(((Microsoft.AspNetCore.Mvc.FileResult)result).FileDownloadName == "Nomination form.pdf");

        }
        [Fact]
        public void DeleteFileTest()
        {
            UploadTest();
            var result = docController.DeleteFileAsync("Nomination form.pdf").Result;
            Assert.True(((Microsoft.AspNetCore.Mvc.ObjectResult)result).Value.ToString() == "File deleted");
           

        }
        [Fact]
        public void DownloadNonExistentFileTest()
        {
            var downloadResult = docController.DownloadFile("xyz.pdf");
            Assert.True(((Microsoft.AspNetCore.Mvc.ObjectResult)downloadResult).Value.ToString() == "File not found");

        }
        private static Mock<IFormFile> createMock(string inputFileName, string fileType)
        {
            var fileMock = new Mock<IFormFile>();
            var physicalFile = new FileInfo(@"Artifacts\"+ inputFileName);
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(physicalFile.OpenRead());
            writer.Flush();
            ms.Position = 0;
            var fileName = physicalFile.Name;
            //Setup mock file using info from physical file
            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(physicalFile.Length);
            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            fileMock.Setup(_ => _.ContentDisposition).Returns(string.Format("inline; filename={0}", fileName));
            fileMock.Setup(_ => _.ContentType).Returns(@"application/"+ fileType);
            return fileMock;
        }
    }
}
