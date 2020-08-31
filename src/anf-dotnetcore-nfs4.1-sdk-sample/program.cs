// Copyright (c) Microsoft and contributors.  All rights reserved.
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.

namespace Microsoft.Azure.Management.ANF.Samples
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.Management.ANF.Samples.Common;
    using Microsoft.Azure.Management.NetApp;
    using Microsoft.Azure.Management.NetApp.Models;
    using static Microsoft.Azure.Management.ANF.Samples.Common.Utils;

    class program
    {
        /// <summary>
        /// Sample console application that creates an ANF Account, Capacity Pool and a Volume enable with NFS 4.1 protocol
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            DisplayConsoleAppHeader();

            try
            {
                RunAsync().GetAwaiter().GetResult();
                Utils.WriteConsoleMessage("Sample application successfuly completed execution.");
            }
            catch (Exception ex)
            {
                WriteErrorMessage(ex.Message);
            }
        }

        static private async Task RunAsync()
        {
            //---------------------------------------------------------------------------------------------------------------------
            // Setting variables necessary for resources creation - change these to appropriated values related to your environment
            //---------------------------------------------------------------------------------------------------------------------
            string subscriptionId = "<subscriptionId>";
            string location = "eastus2";
            string resourceGroupName = "anf01-rg";
            string vnetName = "vnet-02";
            string subnetName = "anf-sn";
            string vnetResourceGroupName = "anf01-rg";
            string anfAccountName = "anfaccount03";
            string capacityPoolName = "Pool01";
            string capacityPoolServiceLevel = "Standard";
            long capacitypoolSize = 4398046511104;  // 4TiB which is minimum size
            long volumeSize = 107374182400;  // 100GiB - volume minimum size

            //----------------------------------------------------------------------------------------
            // Authenticating using service principal, refer to README.md file for requirement details
            //----------------------------------------------------------------------------------------
            WriteConsoleMessage("Authenticating...");
            var credentials = await ServicePrincipalAuth.GetServicePrincipalCredential("AZURE_AUTH_LOCATION");

            //------------------------------------------
            // Instantiating a new ANF management client
            //------------------------------------------
            WriteConsoleMessage("Instantiating a new Azure NetApp Files management client...");
            AzureNetAppFilesManagementClient anfClient = new AzureNetAppFilesManagementClient(credentials) { SubscriptionId = subscriptionId };
            WriteConsoleMessage($"\tApi Version: {anfClient.ApiVersion}");

            //----------------------
            // Creating ANF Account
            //----------------------

            // Setting up NetApp Files account body  object
            NetAppAccount anfAccountBody = new NetAppAccount(location, null, anfAccountName);

            // Requesting account to be created
            WriteConsoleMessage("Requesting account to be created...");
            var anfAccount = await anfClient.Accounts.CreateOrUpdateAsync(anfAccountBody, resourceGroupName, anfAccountName);
            WriteConsoleMessage($"\tAccount Resource Id: {anfAccount.Id}");

            //-----------------------
            // Creating Capacity Pool
            //-----------------------

            // Setting up capacity pool body  object
            CapacityPool capacityPoolBody = new CapacityPool()
            {
                Location = location.ToLower(), // Important: location needs to be lower case
                ServiceLevel = capacityPoolServiceLevel,
                Size = capacitypoolSize
            };

            // Creating capacity pool
            WriteConsoleMessage("Requesting capacity pool to be created...");
            var capacityPool = await anfClient.Pools.CreateOrUpdateAsync(capacityPoolBody, resourceGroupName, anfAccount.Name, capacityPoolName);
            WriteConsoleMessage($"\tCapacity Pool Resource Id: {capacityPool.Id}");

            //------------------------
            // Creating NFS 4.1 Volume
            //------------------------

            // Creating export policy object
            VolumePropertiesExportPolicy exportPolicies = new VolumePropertiesExportPolicy()
            {
                Rules = new List<ExportPolicyRule>
                {
                    new ExportPolicyRule() { 
                        AllowedClients = "0.0.0.0",
                        Cifs = false, 
                        Nfsv3 = false,
                        Nfsv41 = true, 
                        RuleIndex = 1, 
                        UnixReadOnly = false, 
                        UnixReadWrite = true
                    }
                }
            };

            // Creating volume body object
            string subnetId = $"/subscriptions/{subscriptionId}/resourceGroups/{vnetResourceGroupName}/providers/Microsoft.Network/virtualNetworks/{vnetName}/subnets/{subnetName}";
            string volumeName = $"Vol-{anfAccountName}-{capacityPoolName}";

            Volume volumeBody = new Volume()
            {
                ExportPolicy = exportPolicies,
                Location = location.ToLower(),
                ServiceLevel = capacityPoolServiceLevel,
                CreationToken = volumeName,
                SubnetId = subnetId,
                UsageThreshold = volumeSize,
                ProtocolTypes = new List<string>() { "NFSv4.1" }
            };

            // Creating NFS 4.1 volume
            WriteConsoleMessage("Requesting volume to be created...");
            var volume = await anfClient.Volumes.CreateOrUpdateAsync(volumeBody, resourceGroupName, anfAccount.Name, ResourceUriUtils.GetAnfCapacityPool(capacityPool.Id), volumeName);
            WriteConsoleMessage($"\tVolume Resource Id: {volume.Id}");

            //------------------------
            // Cleaning up
            //------------------------
            //WriteConsoleMessage("Cleaning up created resources...");

            //WriteConsoleMessage("\tDeleting volume...");
            //await anfClient.Volumes.DeleteAsync(resourceGroupName, anfAccount.Name, ResourceUriUtils.GetAnfCapacityPool(capacityPool.Id), ResourceUriUtils.GetAnfVolume(volume.Id));
            //// Adding a final verification if the resource completed deletion since it may have a few secs between ARM the Resource Provider be fully in sync
            //await WaitForNoAnfResource<Volume>(anfClient, volume.Id);
            //Utils.WriteConsoleMessage($"\t\tDeleted volume: {volume.Id}");

            //WriteConsoleMessage("\tDeleting capacity pool...");
            //await anfClient.Pools.DeleteAsync(resourceGroupName, anfAccount.Name, ResourceUriUtils.GetAnfCapacityPool(capacityPool.Id));
            //await WaitForNoAnfResource<CapacityPool>(anfClient, capacityPool.Id);
            //Utils.WriteConsoleMessage($"\t\tDeleted capacity pool: {capacityPool.Id}");

            //WriteConsoleMessage("\tDeleting account...");
            //await anfClient.Accounts.DeleteAsync(resourceGroupName, anfAccount.Name);
            //await WaitForNoAnfResource<NetAppAccount>(anfClient, anfAccount.Id);
            //Utils.WriteConsoleMessage($"\t\tDeleted account: {anfAccount.Id}");
        }        
    }
}
