using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SwDocumentMgr;

namespace ReplaceReference
{
    class Program
    {
        static SwDMApplication dmDocManager;
        static SwDMClassFactory dmClassFact;
        //  You must obtain the key directly from SolidWorks API division
        const string SolidWorksDocumentManagerKey = "<Insert Key Here>";


        static void Main(string[] args)
        {
            //string[] args = { @"C:\PracticeFiles\02997.SLDASM", @"C:\PracticeFiles\02967.SLDPRT", @"C:\PracticeFiles\02967 - Copy.SLDPRT" };
            string parentFile;
            string childToReplace;
            string newChild;
            bool quietMode;
            
                //How many input arguments?
                switch (args.Length)
                {
                    case 3:
                        quietMode = false;
                        if (args[0].Contains("*") || args[0].Contains("?"))
                        {
                            inputError(quietMode);
                            return;
                        }
                        parentFile = Path.GetFullPath(args[0]);
                        childToReplace = Path.GetFullPath(args[1]);
                        newChild = Path.GetFullPath(args[2]);
                        break;
                    case 4:
                        if (args[0] != "/q")
                        {
                            quietMode = false;
                            inputError(quietMode);
                            return;
                        }
                        quietMode = true;
                        if (args[1].Contains("*") || args[1].Contains("?"))
                        {
                            inputError(quietMode);
                            return;
                        }
                        parentFile = Path.GetFullPath(args[1]);
                        childToReplace = Path.GetFullPath(args[2]);
                        newChild = Path.GetFullPath(args[3]);
                        break;
                    default:
                        quietMode = false;
                        inputError(quietMode);
                        return;
                }
            

            //Get Parent DocType
            SwDmDocumentType parentDocType = setDocType(parentFile);
            if (parentDocType == SwDmDocumentType.swDmDocumentUnknown)
            {
                inputError(quietMode);
                return;
            }

            //Check New child DocType
            SwDmDocumentType newChildDocType = setDocType(newChild);
            if (newChildDocType == SwDmDocumentType.swDmDocumentDrawing || newChildDocType == SwDmDocumentType.swDmDocumentUnknown)
            {
                Console.WriteLine("\"" + parentFile + "\"\t\"" + "The replacement file (New Child) must be a SolidWorks Part (.prt or .sldprt) or Assembly (.asm or .sldasm).");
                inputError(quietMode);
                return;
            }
            //Check that newChild Exists
            if (File.Exists(newChild) == false)
            {
                Console.WriteLine("\"" + parentFile + "\"\t\"" + "The replacement file does not exist.");
                inputError(quietMode);
                return;

            }

            //Prerequisites
            SwDmDocumentOpenError OpenError;
            dmClassFact = new SwDMClassFactory();
            dmDocManager = dmClassFact.GetApplication(SolidWorksDocumentManagerKey) as SwDMApplication;

            //Open Parent Document
            SwDMDocument15 dmParent = dmDocManager.GetDocument(parentFile, parentDocType, false, out OpenError) as SwDMDocument15;

            //Check that a SolidWorks file is open
            if (dmParent != null)
            {
                //Setup to check that ChildToReplace Exists as a reference
                SwDMSearchOption dmSearchOptions = dmDocManager.GetSearchOptionObject();
                object BrokenRefs;
                string[] refsBefore;
                string[] refsAfter;
                object isVirtual;
                object timeStamp;

                //Check that ChildToReplace Exists as a reference
                refsBefore = dmParent.GetAllExternalReferences4(dmSearchOptions, out BrokenRefs, out isVirtual, out timeStamp) as string[];
                if (refsBefore.Contains(childToReplace) == false)
                {
                    Console.WriteLine("\"" + parentFile + "\"\t\"" + "Does not contain Referehce: " + childToReplace + "\"");
                    inputError(quietMode);
                    dmParent.CloseDoc();
                    return;

                };

                //Attempt to replace the reference
                try
                {
                    dmParent.ReplaceReference(childToReplace, newChild);

                    //Used for Troubleshooting
                    refsAfter = dmParent.GetAllExternalReferences4(dmSearchOptions, out BrokenRefs, out isVirtual, out timeStamp) as string[];

                    //check that reference was replaced
                    if (refsAfter.Contains(newChild) == false)
                    {
                        Console.WriteLine("\"" + parentFile + "\"\t\"" + "The replace failed." + "\"");
                        inputError(quietMode);
                        dmParent.CloseDoc();
                        return;
                    }
                }

                    //If there is an error, the file is not saved.
                catch (Exception e)
                {
                    Console.WriteLine("\"" + parentFile + "\"\t\"" + " Replacing reference failed.  No change was saved. " + "ErrorMessage: " + e.Message + " StackTrace: " + e.StackTrace);
                    dmParent.CloseDoc();
                    inputError(quietMode);
                    return;
                }

                //File is saved upon a succefully replacing the reference.
                SwDmDocumentSaveError SaveError;
                try
                {
                    SaveError = dmParent.Save();
                    if (SaveError != SwDmDocumentSaveError.swDmDocumentSaveErrorNone)
                    {
                        Console.WriteLine("\"" + parentFile + "\"\t\"" + " Failed to save. No changes saved. " + "ErrorMessage: " + SaveError.ToString());
                        inputError(quietMode);
                        dmParent.CloseDoc();
                        return;
                    }
                    dmParent.CloseDoc();
                    Console.WriteLine("\"" + parentFile + "\"\t\"" + "Success. Changes Saved.\"");
                }
                catch (Exception ee)
                {
                    Console.WriteLine("\"" + parentFile + "\"\t\"" + " Failed to save. No changes saved. " + "ErrorMessage: " + ee.Message + " StackTrace: " + ee.StackTrace);
                    return;
                }
            }
            else
            {
                switch (OpenError)
                {
                    case SwDmDocumentOpenError.swDmDocumentOpenErrorFail:
                        Console.WriteLine("\"" + parentFile + "\"\t\"" + "File failed to open; reasons could be related to permissions, the file is in use, or the file is corrupted." + "\"");
                        inputError(quietMode);
                        break;
                    case SwDmDocumentOpenError.swDmDocumentOpenErrorFileNotFound:
                        Console.WriteLine("\"" + parentFile + "\"\t\"" + "File not found" + "\"");
                        inputError(quietMode);
                        break;
                    case SwDmDocumentOpenError.swDmDocumentOpenErrorNonSW:
                        Console.Write("\"" + parentFile + "\"\t\"" + "Non-SolidWorks file was opened" + "\"");
                        inputError(quietMode);
                        break;
                    default:
                        Console.WriteLine("\"" + parentFile + "\"\t\"" + "An unknown errror occurred.  Something is wrong with the code of GetReferences" + "\"");
                        inputError(quietMode);
                        break;
                }

            }


        }




        static void inputError(bool quietMode)
        {
            if (quietMode == true)
                return;
            Console.WriteLine(@"
Syntax 
    [option]    [ParentFilePath]   [ChildFilePath] [NewChildFilePath]

No wildcars allowed. If the path has spaces use quotes around it.  Note that
the files must have one of the following file extensions: .sldasm, .slddrw, 
.sldprt, .asm, .drw, or .prt.  The NewChildFilePath must exist.  The output 
is tab delimited.  This makes it easy to redirect the output to a text file 
that can be opened as spreadsheet.

Options
    /q      Quiet mode.  Suppresses the current message.  It does
            not suppress the one line error messages related to problems
            opening SolidWorks Files.  Quiet mode is useful for batch files
            when you are directing the output to a file.  The main error 
            message is suppressed but you are still informed about problems 
            opening files.

Version 2011-Oct-4 21:30
Written and Maintained by Jason Nicholson
http://github.com/jasonnicholson/ReplaceReference");

        }




        static SwDmDocumentType setDocType(string docPath)
        {
            string fileExtension = Path.GetExtension(docPath);

            switch (fileExtension.ToUpper())
            {
                case ".SLDASM":
                case ".ASM":
                    return SwDmDocumentType.swDmDocumentAssembly;
                case ".SLDDRW":
                case ".DRW":
                    return SwDmDocumentType.swDmDocumentDrawing;
                case ".SLDPRT":
                case ".PRT":
                    return SwDmDocumentType.swDmDocumentPart;
                default:
                    return SwDmDocumentType.swDmDocumentUnknown;
            }

        }
    }
}
