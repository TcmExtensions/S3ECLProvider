using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Amazon.S3;
using Amazon.S3.Model;

namespace S3ECLProvider.API
{
    internal class S3
    {
        private readonly IAmazonS3 _S3Client;
        private readonly String _bucketName;
        private readonly String _bucketUrl;
        private readonly String _prefix;

        private String GetFullPrefix(String prefix)
        {
            if (!prefix.StartsWith(_prefix))
                return _prefix + prefix;
            else
                return prefix;
        }

        private String StripPrefix(String key)
        {
            if (!String.IsNullOrEmpty(_prefix))
                return key.Substring(_prefix.Length);
            else
                return key;
        }

        #region Constructors
        internal S3(string region, string bucketName, string accessKeyId, string secretAccessKey, string bucketUrl, string prefix)
        {
            if (String.IsNullOrEmpty(region))
                throw new ArgumentNullException("S3 Region not specified.");

            if (String.IsNullOrEmpty(bucketName))
                throw new ArgumentNullException("S3 BucketName not specified.");

            if (String.IsNullOrEmpty(accessKeyId))
                throw new ArgumentNullException("S3 AccessKeyId not specified.");

            if (String.IsNullOrEmpty(secretAccessKey))
                throw new ArgumentNullException("S3 SecretAccessKey not specified.");

            if (String.IsNullOrEmpty(bucketUrl))
                throw new ArgumentNullException("S3 BucketUrl not specified.");

            _bucketName = bucketName;
            _bucketUrl = bucketUrl;

            if (!String.IsNullOrEmpty(prefix))
                _prefix = prefix;
            else
                _prefix = String.Empty;

            Amazon.RegionEndpoint regionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region);

            if (regionEndpoint == null)
                throw new ArgumentException(String.Format("Unknown S3 region: \"{0}\".", region), "region");

            _S3Client = new AmazonS3Client(accessKeyId, secretAccessKey, regionEndpoint);

            if (!_S3Client.DoesS3BucketExist(bucketName))
                throw new ArgumentException(String.Format("S3 bucket \"{0}\" does not exist or is not accessible.", bucketName), "bucketName");
        }
        #endregion

        /// <summary>
        /// GetListing generates a list of <see cref="T:Amazon.S3.Model.S3Object" /> within a certain prefix, without recursing by default (folder specific listing)
        /// </summary>
        /// <param name="prefix">Prefix to list</param>
        /// <param name="recursive">Request recursive listing under folder</param>
        /// <returns>List of <see cref="T:Amazon.S3.Model.S3Object" /></returns>
        /// <remarks>In Amazon S3, folders are non-existing items. This function generates pseudo-items for folders.</remarks>
        internal IEnumerable<S3ItemData> GetListing(String prefix, bool recursive = false)
        {
            ListObjectsV2Request request = new ListObjectsV2Request() {
                BucketName = _bucketName,
                FetchOwner = false,
                Prefix = VirtualPathUtility.AppendTrailingSlash(GetFullPrefix(prefix)),
                Delimiter = recursive ? null : "/"
            };

            ListObjectsV2Response response = null;

            do {
                if (response != null && response.IsTruncated)
                    request.ContinuationToken = response.NextContinuationToken;

                response = _S3Client.ListObjectsV2(request);

                foreach (String subPrefix in response.CommonPrefixes) {
                    yield return GetFolder(StripPrefix(subPrefix));
                }

                foreach (S3Object s3Object in response.S3Objects) {
                    yield return new S3ItemData(StripPrefix(s3Object.Key), _bucketUrl, s3Object);
                }

            } while (response != null && response.IsTruncated);
        }

        /// <summary>
        /// Gets a <see cref="S3Object" /> representing a folder
        /// </summary>
        /// <param name="key">Amazon S3 Key</param>
        /// <returns><see cref="T:Amazon.S3.Model.S3Object" /></returns>
        /// <remarks>Folders in Amazon S3 are virtual hence the returned object is shallow wrapper</remarks>
        internal S3ItemData GetFolder(String key)
        {
            return new S3ItemData(VirtualPathUtility.RemoveTrailingSlash(key), _bucketUrl, new S3Object() {
                BucketName = _bucketName,
                Key = _prefix + VirtualPathUtility.RemoveTrailingSlash(key),
                StorageClass = "Folder"
            });
        }

        /// <summary>
        /// Gets the full media URL of the specified AWS S3 asset
        /// </summary>
        /// <param name="key">AWS S3 ECL Key.</param>
        /// <returns>Absolute URL to the asset</returns>
        internal String GetMediaUrl(String key)
        {
            return _bucketUrl + GetFullPrefix(key).Replace("%", "%25");
        }

        /// <summary>
        /// Retrieve <see cref="T:Amazon.S3.Model.S3Object" /> for the specified <paramref name="key" />
        /// </summary>
        /// <param name="key">Amazon S3 Key</param>
        /// <returns><see cref="T:Amazon.S3.Model.S3Object" /></returns>
        internal S3ItemData GetObject(String key)
        {
            ListObjectsV2Request request = new ListObjectsV2Request() {
                BucketName = _bucketName,
                FetchOwner = true,
                Prefix = GetFullPrefix(key),
                MaxKeys = 1
            };

            ListObjectsV2Response response = _S3Client.ListObjectsV2(request);

            if (response != null) {
                S3Object s3Object = response.S3Objects.FirstOrDefault();

                if (s3Object != null) {
                    return new S3ItemData(StripPrefix(s3Object.Key), _bucketUrl, s3Object);
                }
            }

            return null;
        }


        internal void RenameFolder(String sourceKey, String destKey)
        {
            String fullSource = GetFullPrefix(sourceKey);
            String fullDest = GetFullPrefix(destKey);

            ListObjectsV2Request request = new ListObjectsV2Request() {
                BucketName = _bucketName,
                FetchOwner = false,
                Prefix = VirtualPathUtility.AppendTrailingSlash(fullSource),
            };

            ListObjectsV2Response response = null;

            do {
                if (response != null && response.IsTruncated)
                    request.ContinuationToken = response.NextContinuationToken;

                response = _S3Client.ListObjectsV2(request);

                foreach (S3Object s3Object in response.S3Objects) {
                    String newKey = fullDest + s3Object.Key.Substring(fullSource.Length);
                    RenameObject(s3Object.Key, newKey);
                }

            } while (response != null && response.IsTruncated);
        }

        internal void RenameObject(String sourceKey, String destKey)
        {
            CopyObjectRequest request = new CopyObjectRequest() {
                CannedACL = S3CannedACL.PublicRead,
                DestinationBucket = _bucketName,
                DestinationKey = GetFullPrefix(destKey),
                MetadataDirective = S3MetadataDirective.COPY,
                SourceBucket = _bucketName,
                SourceKey = GetFullPrefix(sourceKey)
            };

            S3ItemData destination = GetObject(destKey);

            if (destination != null)
                throw new Exception(String.Format("Destination object with key {0} already exists.", request.DestinationKey));

            CopyObjectResponse response = _S3Client.CopyObject(request);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(String.Format("Failed to copy {0} to {1}", request.SourceKey, request.DestinationKey));

            _S3Client.Delete(_bucketName, request.SourceKey, null);
        }
    }
}
