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

%type <type> program

%%

program				: Program OpenBracket program_content CloseBracket Eof { }

program_content		: declarations instructions { }

instructions		: instructions instruction
					| instruction { }

instruction			: write_instruction { }

write_instruction	: Write IntNumber Semicolon { }
