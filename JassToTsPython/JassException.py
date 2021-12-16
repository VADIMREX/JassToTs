formatMessage = "Line {}, Col {}: {}"
IsStrict = True

class JassException(Exception):
    def __init__(self, line, col, message):
        super(JassException, self).__init__(formatMessage.format(line, col, message))

def Error(line, col, message):
    if IsStrict:
        raise JassException(line, col, message)
    print(formatMessage.format(line, col, message))
