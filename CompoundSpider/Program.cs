using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CompoundSpider
{
    public class Slideshow {
        public string Title { get; set; }
        public string Author { get; set; }
        public string Date { get; set; }

        public Dictionary<string, string>[] Slides { get; set; }
    }
    class Program
    {
        static async Task Main(string[] args)
        {
            long intValue = 0;
            using (var db = new CompoundContext())
            {
                if (db.Settings.Any(s => s.Key == "Last"))
                {
                    var last = db.Settings.Single(s => s.Key == "Last");
                    intValue = long.Parse(last.Value);
                } else
                {
                    db.Settings.Add(new Setting { Key = "Last", Value = "0" });
                    db.SaveChanges();
                }
            }
            for (long i = intValue + 1, j = intValue + 100; i <= j; i++)
            {
                string url = $"https://pubchem.ncbi.nlm.nih.gov/rest/pug_view/data/compound/{i}/JSON/?response_type=display";
                HttpClient client = new HttpClient();
                string responseBody = "";
                try
                {
                    responseBody = await client.GetStringAsync(url);
                    Console.WriteLine("[Info]Processing {0}", i.ToString());
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("[Error]Processing {0}:{1} ", i.ToString(), e.Message);
                    continue;
                }
                client.Dispose();

                JObject jsonData = JObject.Parse(responseBody);
                JToken record_sections = jsonData["Record"]["Section"];
                Compound thisCompound = new Compound();
                thisCompound.CompoundName = "";
                thisCompound.SMILES = "";
                thisCompound.MolecularFormula = "";
                thisCompound.CAS = "";
                thisCompound.MolecularWeight = 0;
                foreach (JToken record_section in record_sections)
                {
                    if (record_section["TOCHeading"].ToString() == "Names and Identifiers")
                    {
                        foreach (JToken record_section_NameandIdentifiers_section in record_section["Section"])
                        {
                            if (record_section_NameandIdentifiers_section["TOCHeading"].ToString() == "Record Title" && thisCompound.CompoundName == "")
                            {
                                thisCompound.CompoundName = record_section_NameandIdentifiers_section["Information"][0]["StringValue"].ToString();
                            }
                            if (record_section_NameandIdentifiers_section["TOCHeading"].ToString() == "Computed Descriptors")
                            {
                                foreach (JToken record_section_NameandIdentifiers_section_ComputedDescriptor_section in record_section_NameandIdentifiers_section["Section"])
                                {
                                    if (record_section_NameandIdentifiers_section_ComputedDescriptor_section["TOCHeading"].ToString() == "IUPAC Name" && thisCompound.CompoundName == "")
                                    {
                                        thisCompound.CompoundName = record_section_NameandIdentifiers_section_ComputedDescriptor_section["Information"][0]["StringValue"].ToString();
                                    }
                                    if (record_section_NameandIdentifiers_section_ComputedDescriptor_section["TOCHeading"].ToString() == "Canonical SMILES")
                                    {
                                        thisCompound.SMILES = record_section_NameandIdentifiers_section_ComputedDescriptor_section["Information"][0]["StringValue"].ToString();
                                    }
                                }
                            }
                            try
                            {
                                if (record_section_NameandIdentifiers_section["TOCHeading"].ToString() == "Molecular Formula")
                                {
                                    thisCompound.MolecularFormula = record_section_NameandIdentifiers_section["Information"][0]["StringValue"].ToString();
                                }
                            }
                            catch
                            {
                                //Mixture? Need Fix
                            }
                            if (record_section_NameandIdentifiers_section["TOCHeading"].ToString() == "Other Identifiers")
                            {
                                foreach (JToken record_section_NameandIdentifiers_section_OtherIdentifiers_section in record_section_NameandIdentifiers_section["Section"])
                                {
                                    if (record_section_NameandIdentifiers_section_OtherIdentifiers_section["TOCHeading"].ToString() == "CAS")
                                    {
                                        thisCompound.CAS = record_section_NameandIdentifiers_section_OtherIdentifiers_section["Information"][0]["StringValue"].ToString();
                                    }
                                }
                            }
                        }
                    }
                    if (record_section["TOCHeading"].ToString() == "Chemical and Physical Properties")
                    {
                        foreach (JToken record_section_ChemicalandPhysicalProperties_section in record_section["Section"])
                        {
                            if (record_section_ChemicalandPhysicalProperties_section["TOCHeading"].ToString() == "Computed Properties")
                            {
                                foreach (JToken record_section_ChemicalandPhysicalProperties_section_Infomation in record_section_ChemicalandPhysicalProperties_section["Information"])
                                {
                                    if (record_section_ChemicalandPhysicalProperties_section_Infomation["Name"].ToString() == "Computed Properties")
                                    {
                                        JToken Rows = record_section_ChemicalandPhysicalProperties_section_Infomation["Table"]["Row"];
                                        foreach (JToken row in Rows)
                                        {
                                            if (row["Cell"][0]["StringValue"].ToString() == "Molecular Weight")
                                            {
                                                thisCompound.MolecularWeight = float.Parse(row["Cell"][1]["NumValue"].ToString());
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                Console.WriteLine("Compound Name : {0}", thisCompound.CompoundName);
                Console.WriteLine("SMILES : {0}", thisCompound.SMILES);
                Console.WriteLine("Molecular Formula : {0}", thisCompound.MolecularFormula);
                Console.WriteLine("CAS : {0}", thisCompound.CAS);
                Console.WriteLine("Mocular Weight : {0}", thisCompound.MolecularWeight);
                using (var db = new CompoundContext())
                {
                    db.Compounds.Add(thisCompound);
                    db.SaveChanges();

                    var last = db.Settings.Single(s => s.Key == "Last");
                    last.Value = i.ToString();
                    db.SaveChanges();
                }
            }
        }
    }
}
