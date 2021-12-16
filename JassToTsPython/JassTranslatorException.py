IsStrict = True

class JassTranslatorException(Exception):
    def __init__(self, message):
        super(JassTranslatorException, self).__init__(message)

def Error(message):
    if IsStrict:
        raise JassTranslatorException(message)
    print(message)
