﻿// Copyright (c) Source Tree Solutions, LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Author:					Joe Audette
// Created:					2015-06-18
// Last Modified:			2015-08-04
// 

using cloudscribe.Configuration;
using cloudscribe.Core.Models;
using Microsoft.AspNet.Hosting;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace cloudscribe.Core.Web.Components
{
    public class ConfigVersionProviderFactory : IVersionProviderFactory
    {
        public ConfigVersionProviderFactory(
            IApplicationEnvironment appEnv,
            IConfiguration configuration,
            ILoggerFactory loggerFactory)
        {
            //logFactory = loggerFactory;
            log = loggerFactory.CreateLogger(typeof(ConfigVersionProviderFactory).FullName);
            //env = hostingEnvironment;
            appBasePath = appEnv.ApplicationBasePath;
            config = configuration;
            configFolderName = config.GetOrDefault("AppSettings:VersionProviderFolderPath", configFolderName);
            didLoadList = LoadList();
        }

        private bool LoadList()
        {
            bool result = false;

            string pathToConfigFolder
                    = appBasePath + configFolderName.Replace("/", Path.DirectorySeparatorChar.ToString());


            if (!Directory.Exists(pathToConfigFolder))
            {
                log.LogError("could not find the folder " + pathToConfigFolder);
                return false;
            }

            DirectoryInfo directoryInfo
                    = new DirectoryInfo(pathToConfigFolder);

            FileInfo[] configFiles = directoryInfo.GetFiles("*.config");

            foreach (FileInfo fileInfo in configFiles)
            {
                XmlDocument configXml = new XmlDocument();
                using (FileStream file = File.OpenRead(fileInfo.FullName))
                {
                    configXml.Load(file);
                    LoadValuesFromConfigurationXml(configXml.DocumentElement);
                    result = true;
                }
                    

            }
            
            return result;
        }


        private string appBasePath;
        private string configFolderName = "/config/codeversionproviders";
       // private IHostingEnvironment env;
        private IConfiguration config;
        private ILogger log;
        private bool didLoadList = false;
        private List<IVersionProvider> versionProviders = new List<IVersionProvider>();

        #region IVersionProviderFactory

        public List<IVersionProvider> VersionProviders
        {
            get {
                if(!didLoadList) { LoadList(); }
                return versionProviders;
            }
        }

        public IVersionProvider Get(string name)
        {
            if (!didLoadList) { LoadList(); }
            foreach(IVersionProvider provider in versionProviders)
            {
                if(provider.Name == name) { return provider; }
            }

            return null;
        }

        #endregion


        private void LoadValuesFromConfigurationXml(XmlNode node)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.Name == "providers")
                {
                    foreach (XmlNode providerNode in child.ChildNodes)
                    {
                        if (
                            (providerNode.NodeType == XmlNodeType.Element)
                            && (providerNode.Name == "add")
                            )
                        {
                            if (
                                (providerNode.Attributes["name"] != null)
                                && (providerNode.Attributes["type"] != null)
                                )
                            {
                                string name = providerNode.Attributes["name"].Value;
                                string type = providerNode.Attributes["type"].Value;

                                try
                                {
                                    object o = Activator.CreateInstance(Type.GetType(type));
                                    if (o is IVersionProvider)
                                    {
                                        versionProviders.Add((IVersionProvider)o);
                                    }
                                }
                                catch(Exception ex)
                                {
                                    log.LogError("could not load version provider " + name + " of type " + type, ex);
                                }
                                
                            }

                        }
                    }

                }
            }
        }

    }

}