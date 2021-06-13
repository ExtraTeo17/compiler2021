%namespace GardensPoint

%union
{
	public string val;
	public char type;
	public SyntaxTree syntaxTree;
	public List<SyntaxTree> syntaxTreeList;
	public Dictionary<string, SymbolInfo> symbolDict;
	public List<Declaration> declList;
	public List<string> idents;
}

%token Program OpenBracket CloseBracket Write Semicolon Eof Comma Hex Assign LogicalSum LogicalProduct Equals NotEquals GreaterThan GreaterOrEqual LessThan LessOrEqual Plus Minus Multiplies Divides BitwiseSum BitwiseProduct BitwiseNegate LogicalNegate OpenPar ClosePar
%token <val> IntNumber StringVar RealNumber BoolValue Ident Int Double Bool

%type <val> typename
%type <syntaxTree> instruction write_instruction exp_instruction unary bitwise factor term relation logical assigner exp ident
%type <syntaxTreeList> instructions
%type <symbolDict> declarations
%type <declList> declaration
%type <idents> identifiers

%%

program				: Program OpenBracket declarations instructions CloseBracket Eof
					{
						Compiler.syntaxTree = new Program($4);
						Compiler.symbolArray = new SymbolArray($3);
					}
					;

declarations		: declarations declaration
					{
						foreach (Declaration decl in $2)
						{
							$1.Add(decl.identifier, decl.symbolInfo);
						}
					}
					|
					{
						$$ = new Dictionary<string, SymbolInfo>();
					}
					;

declaration			: typename identifiers Semicolon
					{
						List<Declaration> declarations = new List<Declaration>();
						foreach (string ident in $2)
						{
							SymbolInfo info = new SymbolInfo($1);
							Declaration declaration = new Declaration(ident, info);
							declarations.Add(declaration);
						}
						$$ = declarations;
					}
					;
					
typename			: Int { }
					| Double { }
					| Bool { }
					;

identifiers			: identifiers Comma Ident
					{
						$$.Add($3);
					}
					| Ident
					{
						$$ = new List<string>();
						$$.Add($1);
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
					| exp_instruction { }
					;

exp_instruction		: exp Semicolon { }
					;

exp					: ident Assign assigner
					{
						$$ = new AssignOperation($1, $3);
					}
					| assigner { }
					;

assigner			: logical LogicalSum logical
					{
						$$ = new LogicalSumOperation($1, $3);
					}
					| logical LogicalProduct logical
					{
						$$ = new LogicalProductOperation($1, $3);
					}
					| logical { }
					;

logical				: relation Equals relation
					{
						$$ = new EqualsOperation($1, $3);
					}
					| relation NotEquals relation
					{
						$$ = new NotEqualsOperation($1, $3);
					}
					| relation GreaterThan relation
					{
						$$ = new GreaterThanOperation($1, $3);
					}
					| relation GreaterOrEqual relation
					{
						$$ = new GreaterOrEqualOperation($1, $3);
					}
					| relation LessThan relation
					{
						$$ = new LessThanOperation($1, $3);
					}
					| relation LessOrEqual relation
					{
						$$ = new LessOrEqualOperation($1, $3);
					}
					| relation { }
					;

relation			: term Plus term
					{
						$$ = new AdditionOperation($1, $3);
					}
					| term Minus term
					{
						$$ = new SubstractionOperation($1, $3);
					}
					| term { }
					;

term				: factor Multiplies factor
					{
						$$ = new MultiplicationOperation($1, $3);
					}
					| factor Divides factor
					{
						$$ = new DivisionOperation($1, $3);
					}
					| factor { }
					;

factor				: bitwise BitwiseSum bitwise
					{
						$$ = new BitwiseSumOperation($1, $3);
					}
					| bitwise BitwiseProduct bitwise
					{
						$$ = new BitwiseProductOperation($1, $3);
					}
					| bitwise { }
					;

bitwise				: Minus unary
					{
						$$ = new UnaryMinusOperation($2);
					}
					| BitwiseNegate unary
					{
						$$ = new BitwiseNegateOperation($2);
					}
					| LogicalNegate unary
					{
						$$ = new LogicalNegateOperation($2);
					}
					| OpenPar Int ClosePar unary
					{
						$$ = new ConvertToIntOperation($4);
					}
					| OpenPar Double ClosePar unary
					{
						$$ = new ConvertToDoubleOperation($4);
					}
					| unary { }
					;

unary				: OpenPar exp ClosePar
					{
						$$ = $2;
					}
					| IntNumber
					{
						$$ = new IntNumber(int.Parse($1));
					}
					| RealNumber
					{
						$$ = new RealNumber(double.Parse($1));
					}
					| BoolValue
					{
						$$ = new BoolValue(bool.Parse($1));
					}
					| ident { }
					;

ident				: Ident
					{
						$$ = new Identifier($1);
					}
					;

write_instruction	: Write exp Semicolon
					{
						$$ = new WriteInstruction($2);
					}
					| Write exp Comma Hex Semicolon
					{
						$$ = new HexWriteInstruction($2);
					}
					| Write StringVar Semicolon
					{
						StringInfo info = Compiler.AddString($2);
						$$ = new StringWriteInstruction(info);
					}
					;

%%

public Parser(Scanner scanner) : base(scanner) { }
