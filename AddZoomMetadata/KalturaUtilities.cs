using Kaltura;
using Kaltura.Enums;
using Kaltura.Services;
using Kaltura.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NLog;

namespace AddZoomMetadata
{
    public class KalturaUtilities
    {
        public Client client;

        public KalturaUtilities(int partnerId, string secret)
        {
            Kaltura.Configuration config = new Kaltura.Configuration();
            config.ServiceUrl = "https://www.kaltura.com/";
            client = new Client(config);
            SessionType type = SessionType.ADMIN;
            int expiry = 86400 * 7; //Sometimes the process can run for a long time.
            string privileges = "disableentitlement,list:*,update:*";
            client.KS = client.GenerateSession(partnerId, secret, "", type, expiry, privileges);
        }

        // Add a tag, "zoom", to a media entry (if one doesn't exist already).
        // If we're in "generateXml" mode, just return a tags XML string; otherwise,
        // make an API call to add the tag.
        public string addZoomTag(MediaEntry entr, bool generateXml)
        {
            string xml = "";
            if (entr.Tags.Contains("zoom") == false)
            {
                Utilities.logInfo(LogLevel.Info, "\tAdd zoom tag");
                if (generateXml)
                {
                    xml = "<tags>";
                    foreach (string tag in entr.Tags.Split(','))
                    {
                        if (string.IsNullOrEmpty(tag)) continue;
                        xml += "<tag>" + tag.Trim() + "</tag>";
                    }
                    xml += "<tag>zoom</tag></tags>";
                }
                else
                {
                    MediaEntry newEntry = new MediaEntry()
                    {
                        Tags = entr.Tags + ",zoom"
                    };
                    var updateResult = MediaService.Update(entr.Id, newEntry)
                                                   .ExecuteAndWaitForResponse(client);
                }
            }
            return xml;
        }

        // This method adds metadata to a media entry in one of two ways:  an API
        // call to do it immediately or creating an XML string which can be added
        // to a Bulk Upload XML file.
        public string addCustomMetadata(MetadataFilter filt,
                                        MediaEntry entr,
                                        bool generateXml,
                                        int targetMetadataProfileId)
        {
            string xml = "";
            filt.ObjectIdEqual = entr.Id;
            var result = MetadataService.List(filt, null)
                                        .ExecuteAndWaitForResponse(client);
            if (result.Objects.Count == 0)
            {
                // There isn't a metadata record for this {entry Id, metadataProfileId}.
                // Add one.
                Utilities.logInfo(LogLevel.Info, "\tAdd Zoom Recording metadata record.");
                if (generateXml)
                {
                    xml = "<customDataItems><action>update</action><customData metadataProfileId=\""
                          + targetMetadataProfileId
                          + "\" metadataProfile=\"PSU_Custom_Metadata\"><xmlData><metadata><MediaType>Zoom Recording</MediaType></metadata></xmlData></customData></customDataItems>";
                }
                else
                {
                    xml = "<metadata><MediaType>Zoom Recording</MediaType></metadata>";
                    var result1 = MetadataService.Add(filt.MetadataProfileIdEqual,
                                                      MetadataObjectType.ENTRY,
                                                      entr.Id,
                                                      xml)
                                                 .ExecuteAndWaitForResponse(client);
                }
            }
            else
            {
                // There is already a Custom Metadata record matching our target Profile ID.
                // If the XML already contains a node for "Zoom Recording", do nothing;
                // otherwise, insert one.
                Metadata md = result.Objects[0];
                if (md.Xml.Contains("Zoom Recording") == false)
                {
                    Utilities.logInfo(LogLevel.Info, "\tUpdate Zoom Recording metadata record.");

                    // The information is stored as an XML string.  Load the string as an
                    // XML "document", then add a new XML node:  <MediaType>.
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(md.Xml);

                    XmlNode root = doc.DocumentElement;

                    //Create a new node.
                    XmlElement elem = doc.CreateElement("MediaType");
                    elem.InnerText = "Zoom Recording";

                    // Add the node to the XML.
                    // NOTE:  The XML is apparently order dependent.  You may have to do
                    // some gymnastics here to insert your tag in the correct place.  The
                    // "MediaType" element must be first in the list if included at all.
                    root.InsertBefore(elem, root.FirstChild);
                    if (generateXml)
                    {
                        string outer = doc.OuterXml.Replace("<?xml version=\"1.0\"?>", "");
                        xml = "<customDataItems><action>update</action><customData metadataProfileId=\""
                              + targetMetadataProfileId
                              + "\" metadataProfile=\"PSU_Custom_Metadata\"><xmlData>"
                              + outer
                              + "</xmlData></customData></customDataItems>";
                    }
                    else
                    {
                        var result1 = MetadataService.Update(md.Id, doc.OuterXml)
                                                     .ExecuteAndWaitForResponse(client);
                    }
                }
            }
            return xml;
        }
    }
}
