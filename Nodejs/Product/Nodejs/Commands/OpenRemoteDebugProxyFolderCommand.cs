﻿//*********************************************************//
//    Copyright (c) Microsoft. All rights reserved.
//    
//    Apache 2.0 License
//    
//    You may obtain a copy of the License at
//    http://www.apache.org/licenses/LICENSE-2.0
//    
//    Unless required by applicable law or agreed to in writing, software 
//    distributed under the License is distributed on an "AS IS" BASIS, 
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or 
//    implied. See the License for the specific language governing 
//    permissions and limitations under the License.
//
//*********************************************************//

using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.VisualStudioTools;

namespace Microsoft.NodejsTools.Commands {
    internal sealed class OpenRemoteDebugProxyFolderCommand : Command {
        public override void DoCommand(object sender, EventArgs args) {
            // Open explorer to folder
            var filePath = Path.Combine(NodejsPackage.RemoteDebugProxyFolder, "RemoteDebug.js");
            if (!File.Exists(filePath)) {
                MessageBox.Show(String.Format("Remote Debug Proxy \"{0}\" does not exist.", filePath), "Node.js Tools for Visual Studio");
            } else {
                Process.Start("explorer", string.Format("/e,/select,{0}", filePath));
            }
        }

        public override int CommandId {
            get { return (int)PkgCmdId.cmdidOpenRemoteDebugProxyFolder; }
        }
    }
}
