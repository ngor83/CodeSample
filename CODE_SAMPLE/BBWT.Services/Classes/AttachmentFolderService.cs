namespace BBWT.Services.Classes
{
    using System.Collections.Generic;
    using System.IO;

    using BBWT.Services.Interfaces;

    using Microsoft.Exchange.WebServices.Data;

    /// <summary>
    /// The attachment folder service.
    /// </summary>
    public class AttachmentFolderService : IAttachmentFolderService
    {
        private readonly IConfigService configService;

        private readonly string rootPath;
        private readonly string archivePath;

        /// <summary>
        /// Initializes ReadFolder service.
        /// </summary>
        /// <param name="configService">The config service.</param>
        public AttachmentFolderService(IConfigService configService)
        {
            this.configService = configService;
            this.rootPath = this.configService.Settings.ExternalFolder.Path + "\\";
            this.archivePath = this.rootPath + "\\Archive\\";
        }

        /// <summary>
        /// Creates directory under the Root attachment folder. 
        /// Will move existing one, if it were archived. 
        /// Will do nothing if folder already exists.
        /// </summary>
        /// <param name="subFolderName">
        /// The sub folder name.
        /// </param>
        /// <returns>
        /// The <see cref="DirectoryInfo"/>.
        /// </returns>
        public DirectoryInfo Create(string subFolderName)
        {
            var folderName = this.rootPath + subFolderName;

            // no need to create directory if it already exists
            if (Directory.Exists(folderName))
            {
                return new DirectoryInfo(folderName);
            }

            // move directory from archive if it exists
            if (Directory.Exists(this.archivePath + subFolderName))
            {
                Directory.Move(this.archivePath + subFolderName, folderName);
                return new DirectoryInfo(folderName);
            }

            // only create new one if it doesn't exists or archived
            return Directory.CreateDirectory(folderName);
        }

        /// <summary>
        /// The rename.
        /// </summary>
        /// <param name="oldName">
        /// The old name.
        /// </param>
        /// <param name="newName">
        /// The new name.
        /// </param>
        /// <returns>
        /// The <see cref="DirectoryInfo"/>.
        /// </returns>
        public DirectoryInfo Rename(string oldName, string newName)
        {
            if (oldName == newName) return new DirectoryInfo(this.rootPath + newName);

            if (!Directory.Exists(this.rootPath + oldName))
            {
                this.Create(this.rootPath + oldName);
            }

            Directory.Move(this.rootPath + oldName, this.rootPath + newName);

            return new DirectoryInfo(this.rootPath + newName);
        }

        /// <summary>
        /// The archive.
        /// </summary>
        /// <param name="subFolderName">
        /// The sub folder name.
        /// </param>
        /// <returns>
        /// The <see cref="DirectoryInfo"/>.
        /// </returns>
        public DirectoryInfo Archive(string subFolderName)
        {
            if (!Directory.Exists(this.archivePath))
            {
                Directory.CreateDirectory(this.archivePath);
            }

            return this.Rename(subFolderName, "\\Archive\\" + subFolderName);
        }

        public bool Exists(string subFolderName)
        {
            return Directory.Exists(this.rootPath + subFolderName);
        }

        /// <summary>
        /// The get certificates file list.
        /// </summary>
        /// <param name="subFolderName">
        /// The sub folder name.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable"/>.
        /// </returns>
        public IEnumerable<FileInfo> GetCertificatesFileList(string subFolderName)
        {
            var folderPath = this.rootPath + subFolderName + "\\" + this.configService.Settings.ExternalFolder.CertificateSubfolderName;

            if (!Directory.Exists(folderPath))
            {
                return new List<FileInfo>();
            }

            return new DirectoryInfo(folderPath).GetFiles();
        }

        /// <summary>
        /// The get photos list.
        /// </summary>
        /// <param name="subFolderName">
        /// The sub folder name.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable"/>.
        /// </returns>
        public IEnumerable<FileInfo> GetPhotosList(string subFolderName)
        {
            var folderPath = this.rootPath + subFolderName + "\\" + this.configService.Settings.ExternalFolder.PhotoSubfolderName;

            if (!Directory.Exists(folderPath))
            {
                return new List<FileInfo>();
            }

            return new DirectoryInfo(folderPath).GetFiles();
        }

        /// <summary>
        /// The get cert file stream.
        /// </summary>
        /// <param name="subFolderName">
        /// The sub folder name.
        /// </param>
        /// <param name="fileName">
        /// The file name.
        /// </param>
        /// <returns>
        /// The <see cref="FileStream"/>.
        /// </returns>
        /// <exception cref="FileNotFoundException">
        /// File not found
        /// </exception>
        public FileStream GetCertFileStream(string subFolderName, string fileName)
        {
            var filePath = this.rootPath + subFolderName + "\\" + this.configService.Settings.ExternalFolder.CertificateSubfolderName + "\\" + fileName;

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException();
            }

            return new FileStream(filePath, FileMode.Open);
        }

        /// <summary>
        /// The get photo file stream.
        /// </summary>
        /// <param name="subFolderName">
        /// The sub folder name.
        /// </param>
        /// <param name="fileName">
        /// The file name.
        /// </param>
        /// <returns>
        /// The <see cref="FileStream"/>.
        /// </returns>
        /// <exception cref="FileNotFoundException">
        /// File not found
        /// </exception>
        public FileStream GetPhotoFileStream(string subFolderName, string fileName)
        {
            var filePath = this.rootPath + subFolderName + "\\" + this.configService.Settings.ExternalFolder.PhotoSubfolderName + "\\" + fileName;

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException();
            }

            return new FileStream(filePath, FileMode.Open);
        }
        
    }
}
