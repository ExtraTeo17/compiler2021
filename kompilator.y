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

%token Program OpenBracket CloseBracket Write Semicolon Eof
%token <val> IntNumber

%type <syntaxTree> instruction write_instruction
%type <syntaxTreeList> declarations instructions

%%

program				: Program OpenBracket declarations instructions Eof
					{
						Compiler.syntaxTree = new Program($3, $4);
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
						$$ = new WriteInstruction(int.Parse($2));
					}
					;

%%

public Parser(Scanner scanner) : base(scanner) { }
