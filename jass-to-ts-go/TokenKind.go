package JassToTs

const (
	/** однострочный комментарий */
	lcom = "lcom"
	/**
	 * многострочный комментарий (в JASS отсутствуют)
	 * @deprecated в JASS только однострочные комментарии начинающиеся с //
	 */
	//@Deprecated
	mcom = "mcom"

	/** десятичное целое */
	ndec = "ndec"
	/** восьмеричное целое */
	oct = "oct"
	/** шеснадцатиричное целое в формате 0xNN */
	xhex = "xhex"
	/** шеснадцатиричное целое в формате $NN */
	dhex = "dhex"
	/** действительное */
	real = "real"
	/** шеснадцатиричное из 4х ASCII символов, записавыается в апостравах */
	adec = "adec"

	/** оператор */
	oper = "oper"

	/** строка в кавычках (в JASS единственный тип строк) */
	dstr = "dstr"
	/**
	 * строка в апострофах (, в JASS в апострафах хранятся целые, смотри <see cref="adec"/>)
	 * @deprecated не используется, в JASS единственный тип строк: заключённые в кавычках
	 */
	//@Deprecated
	sstr = "sstr"

	/** идентификатор */
	name = "name"

	/** базовый тип */
	btyp = "btyp"
	/** ключевое слово */
	kwd = "kwd"

	/** null значение */
	null = "null"
	/** булевое значение */
	_bool = "bool"

	/** перевод строки */
	ln = "ln"

	/** левая скобка */
	lbra = "lbra"
	/** правая скобка */
	rbra = "rbra"
	/** левая квадратная скобка */
	lind = "lind"
	/** правая квадратная скобка */
	rind = "rind"

	/** макросы YDWE, пока что преобразуются в комментарий */
	ymacr = "ymacr"
)

var TokenTypeByKind = map[string]string{
	lcom:  comm,
	mcom:  comm,
	ndec:  val,
	oct:   val,
	xhex:  val,
	dhex:  val,
	real:  val,
	adec:  val,
	oper:  oper,
	dstr:  val,
	sstr:  val,
	name:  name,
	btyp:  name,
	kwd:   kwd,
	null:  val,
	_bool: val,
	ln:    br,
	lbra:  par,
	rbra:  par,
	lind:  par,
	rind:  par,

	ymacr: comm,
}

func GetTypeOfKind(kind string) string {
	return TokenTypeByKind[kind]
}