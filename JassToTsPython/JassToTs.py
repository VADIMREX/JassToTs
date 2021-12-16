class JassToTs:
    def __init__(self, isOptimizationNeeded = False, isYdweCompatible = False, IsDTS = False, IndentSize = 4):
        self.isOptimizationNeeded = isOptimizationNeeded
        self.isYdweCompatible = isYdweCompatible
        self.IsDTS = IsDTS
        self.IndentSize = IndentSize

    def Convert(self, tree):
        pass