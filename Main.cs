using System;
using System.IO;
using System.Collections.Generic;
using GardensPoint;

public class Compiler
{
    public static int errors = 0;

    public static SyntaxTree syntaxTree;

    public static List<string> source;

    private static StreamWriter writer;

    public static int Main(string[] args)
    {
        string file;
        FileStream source;
        Console.WriteLine("\nCompiler for MINI language");

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
        EmitCode("; prolog");
        EmitCode("@int_res = constant [15 x i8] c\"  Result:  %d\\0A\\00\"");
        EmitCode("@double_res = constant [16 x i8] c\"  Result:  %lf\\0A\\00\"");
        EmitCode("@end = constant [20 x i8] c\"\\0AEnd of execution\\0A\\0A\\00\"");
        EmitCode("declare i32 @printf(i8*, ...)");
        EmitCode("define void @main()");
        EmitCode("{");
        for (char c = 'a'; c <= 'z'; ++c)
        {
            EmitCode($"%i{c} = alloca i32");
            EmitCode($"store i32 0, i32* %i{c}");
            EmitCode($"%r{c} = alloca double");
            EmitCode($"store double 0.0, double* %r{c}");
        }
        EmitCode();

        syntaxTree.GenCode();

        EmitCode("}");
    }

    public static void EmitCode(string instr = null)
    {
        writer.WriteLine(instr);
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
    private List<SyntaxTree> declarations = new List<SyntaxTree>();
    private List<SyntaxTree> instructions = null;

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
        return null;
    }
}

class WriteInstruction : SyntaxTree
{
    private int value;

    public WriteInstruction(int val)
    {
        value = val;
    }

    public override char CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        Compiler.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([15 x i8]* @int_res to i8*), i32 {value.ToString()})");
        return null;
    }
}
