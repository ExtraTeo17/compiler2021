%namespace GardensPoint

%{

public SyntaxTree tree;

}%

%union
{
	public string val;
	public char type;
}

%token Program OpenBracket CloseBracket Write Eof
%token <val> IntNumber

%%

program				: Program OpenBracket declarations instructions Eof
					{
						Compiler.syntaxTree = new Program($3, $4);
					}

instructions		: instructions instruction
					{
						$1.Add($2);
					}
					| instruction
					{
						$$ = new List<SyntaxTree>();
						$$.Add($1);
					}

instruction			: write_instruction { }

write_instruction	: Write IntNumber Semicolon
					{
						$$ = new WriteInstruction(int.Parse($2));
					}
