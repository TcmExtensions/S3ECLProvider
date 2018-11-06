using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using Tridion.ExternalContentLibrary.DomainModel.Configuration;
using Tridion.Web.UI.Editors.CME.Views.Popups;

namespace S3ECLProvider.Web
{
    public partial class UploadPopup : PopupView
    {
        private static readonly XNamespace S3Ns = "http://www.sdltridion.com/S3EclProvider/Configuration";

        private Dictionary<String, String> _config = new Dictionary<String, String>();

        /// <summary>
        /// Compute and return the hash of a data blob using the specified algorithm and key
        /// </summary>
        /// <param name="algorithm">Algorithm to use for hashing</param>
        /// <param name="key">Hash key</param>
        /// <param name="data">Data blob</param>
        /// <returns>Hash of the data</returns>
        private static byte[] ComputeKeyedHash(byte[] key, byte[] data)
        {
            using (KeyedHashAlgorithm kha = KeyedHashAlgorithm.Create("HMACSHA256")) {
                kha.Key = key;
                return kha.ComputeHash(data);
            }
        }

        private static void WriteCondition(XmlDictionaryWriter writer, String name, String value)
        {
            writer.WriteStartElement("item");
            writer.WriteAttributeString("type", "object");
            writer.WriteStartElement(name);
            writer.WriteValue(value);
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        private static void WriteCondition(XmlDictionaryWriter writer, String operation, String name, String value)
        {
            writer.WriteStartElement("item");
            writer.WriteAttributeString("type", "array");

            writer.WriteStartElement("item");
            writer.WriteAttributeString("type", "string");
            writer.WriteValue(operation);
            writer.WriteEndElement();

            writer.WriteStartElement("item");
            writer.WriteAttributeString("type", "string");
            writer.WriteValue(name);
            writer.WriteEndElement();

            writer.WriteStartElement("item");
            writer.WriteAttributeString("type", "string");
            writer.WriteValue(value);
            writer.WriteEndElement();

            writer.WriteEndElement();
        }

        /// <summary>
        /// Helper to format a byte array into string
        /// </summary>
        /// <param name="data">The data blob to process</param>
        /// <param name="lowercase">If true, returns hex digits in lower case form</param>
        /// <returns>String version of the data</returns>
        private static string ToHexString(byte[] data, bool lowercase)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < data.Length; i++) {
                sb.Append(data[i].ToString(lowercase ? "x2" : "X2"));
            }
            return sb.ToString();
        }

        protected String GetSignature()
        {
            String secretAccessKey = _config["SecretAccessKey"];
            String region = _config["Region"];

            byte[] hashDate = ComputeKeyedHash(Encoding.UTF8.GetBytes("AWS4" + secretAccessKey), Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("yyyyMMdd")));
            byte[] hashRegion = ComputeKeyedHash(hashDate, Encoding.UTF8.GetBytes(region));
            byte[] hashService = ComputeKeyedHash(hashRegion, Encoding.UTF8.GetBytes("s3"));
            byte[] key = ComputeKeyedHash(hashService, Encoding.UTF8.GetBytes("aws4_request"));

            return ToHexString(ComputeKeyedHash(key, Encoding.UTF8.GetBytes(GeneratePolicy())), true);
        }

        protected String GeneratePolicy()
        {
            using (MemoryStream stream = new MemoryStream()) {
                XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(stream);

                writer.WriteStartElement("root");
                writer.WriteAttributeString("type", "object");

                // Expiration date
                writer.WriteStartElement("expiration");
                writer.WriteValue(DateTime.UtcNow.AddHours(3).ToString("yyyy-MM-ddTHH:00:00.000Z"));
                writer.WriteEndElement();

                // Conditions
                writer.WriteStartElement("conditions");
                writer.WriteAttributeString("type", "array");

                // ACL
                WriteCondition(writer, "acl", "public-read");

                // Bucket
                WriteCondition(writer, "bucket", _config["BucketName"]);

                // Cache-Control
                WriteCondition(writer, "Cache-Control", _config["CacheControl"]);

                // Content-Type condition
                WriteCondition(writer, "starts-with", "$Content-Type", String.Empty);

                // Key Condition
                WriteCondition(writer, "starts-with", "$key", VirtualPathUtility.AppendTrailingSlash(_config["Prefix"]));

                // success_action_redirect
                WriteCondition(writer, "success_action_redirect", SuccessRedirect);

                // x-amz-date
                WriteCondition(writer, "x-amz-date", Date);

                // x-amz-algorithm
                WriteCondition(writer, "x-amz-algorithm", "AWS4-HMAC-SHA256");

                // x-amz-credential
                WriteCondition(writer, "x-amz-credential", Credential);

                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.Flush();

                // Return serialized JSON result
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        // Form action: http://examplebucket.s3.amazonaws.com/
        protected String Action
        {
            get {
                return String.Format("https://s3-{0}.amazonaws.com/{1}/", _config["Region"], _config["BucketName"]);
            }
        }

        protected String CacheControl
        {
            get {
                return _config["CacheControl"];
            }
        }

        // AWS Credentials: <your-access-key-id>/<date>/<aws-region>/<aws-service>/aws4_request
        protected String Credential
        {
            get {
                return String.Format("{0}/{1}/{2}/s3/aws4_request", _config["AccessKeyId"], DateTime.UtcNow.ToString("yyyyMMdd"), _config["Region"]);
            }
        }

        // Date in ISO8601 format: 20130728T000000Z.
        protected String Date
        {
            get {
                return DateTime.UtcNow.ToString("yyyyMMdd'T000000Z'");
            }
        }

        protected String Prefix
        {
            get {
                return VirtualPathUtility.AppendTrailingSlash(_config["Prefix"]) + VirtualPathUtility.AppendTrailingSlash(Request.Params["prefix"]) + "<file.ext>";
            }
        }

        protected String SuccessRedirect
        {
            get {
                return Request.Url.GetLeftPart(UriPartial.Path) + "?success=true";
            }
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            if (String.Equals(Request.Params["success"], "true", StringComparison.OrdinalIgnoreCase)) {
                phForm.Visible = false;
                phMessage.Visible = true;

                litMessage.Text = String.Format("File \"{0}\" successfully uploaded to S3 bucket \"{1}\".", Request.Params["key"], Request.Params["bucket"]);

                return;
            }

            foreach (MountPointConfiguration mountConfig in ExternalContentLibraryConfiguration.MountPoints) {
                
                if (String.Equals(mountConfig.Id, Request.Params["id"], StringComparison.OrdinalIgnoreCase)) {

                    XElement s3Config = mountConfig.Root.Element(S3Ns + "S3ECLProvider");

                    if (s3Config == null)
                        throw new ConfigurationErrorsException("No S3 configuration node with namespace http://www.sdltridion.com/S3EclProvider/Configuration found.");

                    _config =
                        s3Config
                            .Elements()
                            .ToDictionary(i => i.Name.LocalName, i => i.Value);

                    return;
                }
            }

            phForm.Visible = false;
            phMessage.Visible = true;

            litMessage.Text = String.Format("Unable to load configuration for mountpoint with id \"{0}\".", Request.Params["id"]);
        }
    }
}