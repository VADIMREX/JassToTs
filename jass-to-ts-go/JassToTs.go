package JassToTs

type JassToTs struct {
	/// <summary> Режим файла описания (*.d.ts) </summary>
	IsDTS bool

	/// <summary> Количество символов в 1 сдвиге </summary>
	IndentSize int

	/// <summary> Режим оптимизации </summary>
	isOptimizationNeeded bool
	/// <summary> Режим совместимости с YDWE </summary>
	isYdweCompatible bool

	//#region for YDWE

	/// <summary> Флаг что найдена 1 из функци YDUserData* </summary>
	isYdwe_YDUserData bool

	//#endregion
}

func NewJassToTs(isOptimizationNeeded bool, isYdweCompatible bool, isDTS bool, indentSize int) *JassToTs {
	return &JassToTs{isDTS, indentSize, isOptimizationNeeded, isYdweCompatible, false}
}

func (this *JassToTs) Convert(stat *Statement) string {
	return ""
}