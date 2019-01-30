namespace BBWT.Services.Interfaces
{
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// The AttachmentFolderService interface.
    /// </summary>
    public interface IAttachmentFolderService
    {
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
        DirectoryInfo Create(string subFolderName);

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
        DirectoryInfo Rename(string oldName, string newName);

        /// <summary>
        /// The archive.
        /// </summary>
        /// <param name="subFolderName">
        /// The sub folder name.
        /// </param>
        /// <returns>
        /// The <see cref="DirectoryInfo"/>.
        /// </returns>
        DirectoryInfo Archive(string subFolderName);

        /// <summary>
        /// Check if sub-folder exists
        /// </summary>
        /// <param name="subFolderName">Sub-folder name</param>
        /// <returns><see cref="System.Boolean"/></returns>
        bool Exists(string subFolderName);

        /// <summary>
        /// The get certificates file list.
        /// </summary>
        /// <param name="subFolderName">
        /// The sub folder name.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable"/>.
        /// </returns>
        IEnumerable<FileInfo> GetCertificatesFileList(string subFolderName);

        /// <summary>
        /// The get photos list.
        /// </summary>
        /// <param name="subFolderName">
        /// The sub folder name.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable"/>.
        /// </returns>
        IEnumerable<FileInfo> GetPhotosList(string subFolderName);

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
        FileStream GetCertFileStream(string subFolderName, string fileName);

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
        FileStream GetPhotoFileStream(string subFolderName, string fileName);
    }
}
