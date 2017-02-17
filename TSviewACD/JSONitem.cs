using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TSviewACD
{

    [DataContract]
    public class getEndpoint_Info
    {
        [DataMember]
        public bool? customerExists;
        [DataMember]
        public string contentUrl;
        [DataMember]
        public string metadataUrl;
    }

    [DataContract]
    public class getAccountInfo_Info
    {
        [DataMember]
        public string termsOfUse;
        [DataMember]
        public string status;
    }

    [Serializable]
    [DataContract]
    public class FileMetadata_Info
    {
        [DataMember]
        public string eTagResponse;
        [DataMember]
        public string id;
        [DataMember]
        public string name;
        [DataMember]
        public string kind;
        [DataMember]
        public int? version;
        [DataMember(Name = "modifiedDate")]
        public string modifiedDate_prop
        {
            get { return modifiedDate_str; }
            set
            {
                modifiedDate = DateTime.Parse(value);
                modifiedDate_str = value;
            }
        }
        private string modifiedDate_str;
        public DateTime modifiedDate;
        [DataMember(Name = "createdDate")]
        public string createdDate_prop
        {
            get { return createdDate_str; }
            set
            {
                createdDate = DateTime.Parse(value);
                createdDate_str = value;
            }
        }
        private string createdDate_str;
        public DateTime createdDate;
        [DataMember]
        public string[] labels;
        [DataMember]
        public string description;
        [DataMember]
        public string createdBy;
        [DataMember]
        public string[] parents;
        [DataMember]
        public string status;
        [DataMember]
        public string tempLink;
        [DataMember]
        public bool? restricted;
        [DataMember]
        public bool? isRoot;
        [DataMember]
        public bool? isShared;

        [DataMember]
        public contentProperties_Info contentProperties;

        public long? OrignalLength
        {
            get
            {
                if (name.StartsWith(Config.CarotDAV_CryptNameHeader))
                {
                    return contentProperties?.size - (CryptCarotDAV.BlockSizeByte + CryptCarotDAV.CryptFooterByte + CryptCarotDAV.CryptFooterByte);
                }
                return contentProperties?.size;
            }
        }
    }

    [Serializable]
    [DataContract]
    public class contentProperties_Info
    {
        [DataMember]
        public int? version;
        [DataMember]
        public string md5;
        [DataMember]
        public long? size;
        [DataMember]
        public string contentType;
        [DataMember]
        public string extension;
    }

    [DataContract]
    public class FileListdata_Info
    {
        [DataMember]
        public long? count;
        [DataMember]
        public string nextToken;
        [DataMember]
        public FileMetadata_Info[] data;
    }


    [DataContract]
    public class Changes_Info
    {
        [DataMember]
        public string checkpoint;
        [DataMember]
        public bool? end;
        [DataMember]
        public bool? reset;
        [DataMember]
        public int? statusCode;
        [DataMember]
        public FileMetadata_Info[] nodes;
    }

    [Serializable]
    [DataContract]
    public class DriveData_Info
    {
        [DataMember]
        public string checkpoint;
        [DataMember]
        public FileMetadata_Info[] nodes;
    }
}
