using System;
using System.IO;
using System.Collections.Generic;
using GardensPoint;
using System.Text;

public class Compiler
{
    public static int errors = 0;

    public static SyntaxTree syntaxTree;

    public static List<string> source;

    private static StreamWriter writer;

    private static List<StringInfo> stringInfos = new List<StringInfo>();

    private static int stringVarNameId = 1;

    public static int Main(string[] args)
    {
        string file;
        FileStream source;
        Console.WriteLine("\nA Compiler for MINI language");

        if (args.Length >= 1)
        {
            file = args[0];
        }
        else
        {
            Console.WriteLine("Please provide filename for compilation!");
            return 1; // TODO: make sure it can be left like that
        }

        try
        {
            var reader = new StreamReader(file);
            string fileContent = reader.ReadToEnd();
            reader.Close();
            Compiler.source = new List<string>(fileContent.Split(new string[] { "\r\n" }, System.StringSplitOptions.None));
            source = new FileStream(file, FileMode.Open);
        }
        catch (Exception e)
        {
            Console.WriteLine("\n" + e.Message);
            return 2;
        }

        Scanner scanner = new Scanner(source);
        Parser parser = new Parser(scanner);
        Console.WriteLine();
        parser.Parse();
        source.Close();

        if (errors > 0)
        {
            Console.WriteLine($"\n {errors} errors detected\n");
        }
        else
        {
            writer = new StreamWriter(file + ".ll");
            GenCode();
            writer.Close();
            Console.WriteLine(" compilation successful\n");
        }

        return errors == 0 ? 0 : 3;
    }

    private static void GenCode()
    {
        EmitCode("@int_print = constant [3 x i8] c\"%d\\00\"");
        EmitCode("@hex_int_print = constant [5 x i8] c\"0X%X\\00\"");
        EmitCode("@double_print = constant [4 x i8] c\"%lf\\00\"");
        EmitCode("@bool_print_true = constant [5 x i8] c\"True\\00\"");
        EmitCode("@bool_print_false = constant [6 x i8] c\"False\\00\"");
        foreach (StringInfo info in stringInfos)
        {
            EmitCode($"@{info.stringVarName} = constant [{info.stringLength + 1} x i8] c\"{info.stringValue}\\00\"");
        }
        EmitCode("declare i32 @printf(i8*, ...)");
        EmitCode();
        EmitCode("define void @main()");
        EmitCode("{");
        syntaxTree.GenCode();
        EmitCode("}");
    }

    public static void EmitCode(string instr = null)
    {
        writer.WriteLine(instr);
    }

    public static StringInfo AddString(string stringValue)
    {
        stringValue = stringValue.Substring(1, stringValue.Length - 2);
        stringValue = UnescapeNewlineQuotasAndBackslash(stringValue);
        int stringLength = stringValue.Length;
        StringBuilder stringBuilder = new StringBuilder();
        foreach (char charValue in stringValue)
        {
            stringBuilder.Append("\\" + ((int)charValue).ToString("X2"));
        }
        stringValue = stringBuilder.ToString();
        StringInfo info = new StringInfo(GetNextStringVarName(), stringLength, stringValue);
        stringInfos.Add(info);
        return info;
    }

    private static string UnescapeNewlineQuotasAndBackslash(string value)
    {
        List<char> unescapedCharArray = new List<char>();
        char[] charArray = value.ToCharArray();
        for (int i = 0; i < charArray.Length; ++i)
        {
            if (charArray[i] == '\\') // TODO: handle the impossible case when backslash as last char
            {
                if (charArray[i + 1] == 'n')
                {
                    unescapedCharArray.Add('\n');
                }
                else
                {
                    unescapedCharArray.Add(charArray[i + 1]);
                }
                i++;
                continue;
            }
            unescapedCharArray.Add(charArray[i]);
        }
        return new string(unescapedCharArray.ToArray());
    }

    internal static string GetNextStringVarName()
    {
        return "string" + stringVarNameId++;
    }
}

public class StringInfo
{
    public string stringVarName;
    public int stringLength;
    public string stringValue;

    public StringInfo(string varName, int strLen, string strVal)
    {
        stringVarName = varName;
        stringLength = strLen;
        stringValue = strVal;
    }
}

public abstract class SyntaxTree
{
    public char type;
    public int line = -1;
    public abstract char CheckType();
    public abstract string GenCode();
}

class Program : SyntaxTree
{
    private List<SyntaxTree> declarations;
    private List<SyntaxTree> instructions;

    public Program(List<SyntaxTree> declList, List<SyntaxTree> instrList)
    {
        declarations = declList;
        instructions = instrList;
    }

    public override char CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        foreach (var decl in declarations)
        {
            decl.GenCode();
        }
        foreach (var instr in instructions)
        {
            instr.GenCode();
        }
        Compiler.EmitCode("ret void");
        return null;
    }
}

class IntWriteInstruction : SyntaxTree
{
    private int value;

    public IntWriteInstruction(int val)
    {
        value = val;
    }

    public override char CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        Compiler.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([3 x i8]* @int_print to i8*), i32 {value.ToString()})");
        return null;
    }
}

class IntHexWriteInstruction : SyntaxTree
{
    private int value;

    public IntHexWriteInstruction(int val)
    {
        value = val;
    }

    public override char CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        Compiler.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([5 x i8]* @hex_int_print to i8*), i32 {value.ToString()})");
        return null;
    }
}

class DoubleWriteInstruction : SyntaxTree
{
    private double value;

    public DoubleWriteInstruction(double val)
    {
        value = val;
    }

    public override char CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        Compiler.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([4 x i8]* @double_print to i8*), double {value.ToString()})");
        return null;
    }
}

class BooleanWriteInstruction : SyntaxTree
{
    private bool value;

    public BooleanWriteInstruction(bool val)
    {
        value = val;
    }

    public override char CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        if (value)
            Compiler.EmitCode("call i32 (i8*, ...) @printf(i8* bitcast ([5 x i8]* @bool_print_true to i8*))");
        else
            Compiler.EmitCode("call i32 (i8*, ...) @printf(i8* bitcast ([6 x i8]* @bool_print_false to i8*))");
        return null;
    }
}

class StringWriteInstruction : SyntaxTree
{
    private StringInfo stringInfo;

    public StringWriteInstruction(StringInfo info)
    {
        stringInfo = info;
    }

    public override char CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        Compiler.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([{stringInfo.stringLength + 1} x i8]* @{stringInfo.stringVarName} to i8*))");
        return null;
    }
}
