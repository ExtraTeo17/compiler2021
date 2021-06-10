
// Uwaga: W wywo³aniu generatora gppg nale¿y u¿yæ opcji /gplex

%namespace GardensPoint

%{

public SyntaxTree tree;

}%

%union
{
	public string val;
	public char type;
}

%token Print 

%%

start		: start line { ++lineno; }
			| line { ++lineno; }

line		: Print exp Endl
				{
					tree = new Print($2);
				}
