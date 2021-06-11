%namespace GardensPoint

%{

public SyntaxTree tree;

%}

%union
{
	public string val;
	public char type;
	public SyntaxTree syntaxTree;
	public List<SyntaxTree> syntaxTreeList;
}

%token Program OpenBracket CloseBracket Write Semicolon Eof Comma Hex
%token <val> IntNumber StringVar

%type <syntaxTree> instruction write_instruction
%type <syntaxTreeList> declarations instructions

%%

program				: Program OpenBracket declarations instructions CloseBracket Eof
					{
						Compiler.syntaxTree = new Program(new List<SyntaxTree>(), $4);
					}
					;

declarations		: { }
					;

instructions		: instructions instruction
					{
						$1.Add($2);
					}
					| instruction
					{
						$$ = new List<SyntaxTree>();
						$$.Add($1);
					}
					;

instruction			: write_instruction { }
					;

write_instruction	: Write IntNumber Semicolon
					{
						$$ = new DecimalWriteInstruction(int.Parse($2));
					}
					| Write IntNumber Comma Hex Semicolon
					{
						$$ = new HexWriteInstruction(int.Parse($2));
					}
					| Write StringVar Semicolon
					{
						StringInfo info = Compiler.AddString($2);
						$$ = new StringWriteInstruction(info);
					}
					;

%%

public Parser(Scanner scanner) : base(scanner) { }
