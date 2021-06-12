%namespace GardensPoint

%union
{
	public string val;
	public char type;
	public SyntaxTree syntaxTree;
	public List<SyntaxTree> syntaxTreeList;
}

%token Program OpenBracket CloseBracket Write Semicolon Eof Comma Hex
%token <val> IntNumber StringVar RealNumber Boolean TypeName Ident

%type <syntaxTree> instruction write_instruction declaration
%type <syntaxTreeList> declarations instructions

%%

program				: Program OpenBracket declarations instructions CloseBracket Eof
					{
						Compiler.syntaxTree = new Program(new List<SyntaxTree>(), $4);
					}
					;

declarations		: declarations declaration
					{

					}
					|
					{
					
					}
					;

declaration			: TypeName identifiers Semicolon
					{
						
					}
					;
					
identifiers			: identifiers Comma Ident
					{
					
					}
					| Ident
					{
					
					}
					;

instructions		: instructions instruction
					{
						$1.Add($2);
					}
					|
					{
						$$ = new List<SyntaxTree>();
					}
					;

instruction			: write_instruction { }
					;

write_instruction	: Write IntNumber Semicolon
					{
						$$ = new IntWriteInstruction(int.Parse($2));
					}
					| Write IntNumber Comma Hex Semicolon
					{
						$$ = new IntHexWriteInstruction(int.Parse($2));
					}
					| Write RealNumber Semicolon
					{
						$$ = new DoubleWriteInstruction(double.Parse($2));
					}
					| Write Boolean Semicolon
					{
						$$ = new BooleanWriteInstruction(bool.Parse($2));
					}
					| Write StringVar Semicolon
					{
						StringInfo info = Compiler.AddString($2);
						$$ = new StringWriteInstruction(info);
					}
					;

%%

public Parser(Scanner scanner) : base(scanner) { }
