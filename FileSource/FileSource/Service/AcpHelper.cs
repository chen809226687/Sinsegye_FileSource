using acp_idl.readwrite;
using filesrc;
using Google.Protobuf;
using Org.BouncyCastle.Utilities;
using sinsegye.acpsharp.acp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static sinsegye.acpsharp.acp.native.ACPNativeMethods;

namespace FileSource.Service
{
    public class AcpHelper
    {
        private AcpClient acpClient;
        private ushort filePort = 502;
        enum fileIdOffset { fileIdGroup =0, upData = 1, openOnline = 2, closeOnline = 3, TriggerImage = 4 };


        public AcpHelper(string TargetId)
        {
            acpClient = new AcpClient(TargetId, filePort);
        }


        public AcpErrorCode UpdataConfig(string Dirpath,out string error)
        {
            error = "";
            FileSrcUpdateReq fileSrcUpdateReq = new FileSrcUpdateReq();
            fileSrcUpdateReq.Dirpath.Add(Dirpath);
            byte[] reqBytes = fileSrcUpdateReq.ToByteArray();
            byte[] bytes;
            var result = acpClient.AcpCall(filePort, (uint)fileIdOffset.fileIdGroup, (uint)fileIdOffset.upData, reqBytes, out bytes, 2000, 2000);
            FileSrcResp fileSrcResp = new FileSrcResp();
            if (bytes != null)
            {
                fileSrcResp.MergeFrom(bytes);
            }
            if (!fileSrcResp.Result)
            {
                error = fileSrcResp.ErrMessage;
            }

            return result;
        }

        public AcpErrorCode OpenOnline(string objectId)
        {
            FileSrcOperationReq fileSrcOperationReq = new FileSrcOperationReq()
            {
                ObjId = objectId
            };
            byte[] reqBytes = fileSrcOperationReq.ToByteArray();
            byte[] bytes;
            var result = acpClient.AcpCall(filePort, (uint)fileIdOffset.fileIdGroup, (uint)fileIdOffset.openOnline, reqBytes, out bytes, 2000, 2000);

            FileSrcResp fileSrcResp = new FileSrcResp();
            if (bytes != null)
            {
                fileSrcResp.MergeFrom(bytes);
            }
            return result;

        }

        public AcpErrorCode CloseOnline(string objectId)
        {
            FileSrcOperationReq fileSrcOperationReq = new FileSrcOperationReq()
            {
                ObjId = objectId
            };
            byte[] reqBytes = fileSrcOperationReq.ToByteArray();
            byte[] bytes;
            var result = acpClient.AcpCall(filePort, (uint)fileIdOffset.fileIdGroup, (uint)fileIdOffset.closeOnline, reqBytes, out bytes, 2000, 2000);
            FileSrcResp fileSrcResp = new FileSrcResp();
            if (bytes != null)
            {
                fileSrcResp.MergeFrom(bytes);
            }
            return result;
        }

        public AcpErrorCode TriggerImage(string objectId)
        {
            FileSrcOperationReq fileSrcOperationReq = new FileSrcOperationReq()
            {
                ObjId = objectId
            };
            byte[] reqBytes = fileSrcOperationReq.ToByteArray();
            byte[] bytes;
            var result = acpClient.AcpCall(filePort, (uint)fileIdOffset.fileIdGroup, (uint)fileIdOffset.TriggerImage, reqBytes, out bytes, 2000, 2000);
            FileSrcResp fileSrcResp = new FileSrcResp();
            if (bytes != null)
            {
                fileSrcResp.MergeFrom(bytes);
            }
            return result;
        }

    }
}
