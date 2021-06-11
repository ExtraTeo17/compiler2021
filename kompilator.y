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
					| instruction { }

instruction			: write_instruction { }

write_instruction	: Write IntNumber Semicolon { }
