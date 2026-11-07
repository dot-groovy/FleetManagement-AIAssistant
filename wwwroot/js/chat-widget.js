(() => {
    const CHAVE_HISTORICO = 'chat-historico';
    const CHAVE_ABERTO = 'chat-aberto';
    const CHAVE_IDIOMA = 'chat-idioma';

    const TEXTOS = {
        pt: {
            titulo: 'Assistente IA',
            limpar: 'Limpar',
            limparTitulo: 'Limpar conversa',
            fechar: 'Fechar',
            placeholder: 'Pergunte sobre a frota...',
            enviar: 'Enviar',
            boasVindas: 'Olá! Pergunte-me sobre veículos, motoristas ou custos de manutenção.',
            limpo: 'Conversa limpa. Em que posso ajudar?',
            idiomaAlterado: 'Idioma alterado para português. Em que posso ajudar?',
            aPensar: 'A pensar...',
            indisponivel: 'O assistente não está disponível. Verifique se o Ollama está em execução.',
            erroResposta: 'Não foi possível obter resposta.',
            erroLigacao: 'Erro de ligação ao servidor.'
        },
        en: {
            titulo: 'AI Assistant',
            limpar: 'Clear',
            limparTitulo: 'Clear conversation',
            fechar: 'Close',
            placeholder: 'Ask about the fleet...',
            enviar: 'Send',
            boasVindas: 'Hi! Ask me about vehicles, drivers or maintenance costs.',
            limpo: 'Conversation cleared. How can I help?',
            idiomaAlterado: 'Language changed to English. How can I help?',
            aPensar: 'Thinking...',
            indisponivel: 'The assistant is not available. Check that Ollama is running.',
            erroResposta: 'Could not get a response.',
            erroLigacao: 'Server connection error.'
        }
    };

    const widget = document.getElementById('chat-widget');
    if (!widget) {
        // Sem widget = sem sessão iniciada: apaga a conversa do utilizador anterior
        sessionStorage.removeItem(CHAVE_HISTORICO);
        return;
    }

    const painel = document.getElementById('chat-painel');
    const mensagens = document.getElementById('chat-mensagens');
    const form = document.getElementById('chat-form');
    const input = document.getElementById('chat-input');
    const botaoEnviar = document.getElementById('chat-enviar');
    const botaoToggle = document.getElementById('chat-toggle');
    const botaoLimpar = document.getElementById('chat-limpar');
    const botaoFechar = document.getElementById('chat-fechar');
    const botoesIdioma = widget.querySelectorAll('.chat-idioma button');
    const token = widget.querySelector('input[name="__RequestVerificationToken"]').value;

    let historico = JSON.parse(sessionStorage.getItem(CHAVE_HISTORICO) ?? '[]');
    let idioma = sessionStorage.getItem(CHAVE_IDIOMA) === 'en' ? 'en' : 'pt';

    // textContent evita XSS
    function desenhar(tipo, texto) {
        const div = document.createElement('div');
        div.className = `chat-msg ${tipo}`;
        div.textContent = texto;
        mensagens.append(div);
        mensagens.scrollTop = mensagens.scrollHeight;
        return div;
    }

    function adicionar(tipo, texto) {
        historico.push({ tipo, texto });
        sessionStorage.setItem(CHAVE_HISTORICO, JSON.stringify(historico));
        desenhar(tipo, texto);
    }

    function aplicarTextos() {
        const t = TEXTOS[idioma];
        document.getElementById('chat-titulo').textContent = t.titulo;
        botaoToggle.textContent = t.titulo;
        botaoLimpar.textContent = t.limpar;
        botaoLimpar.title = t.limparTitulo;
        botaoFechar.setAttribute('aria-label', t.fechar);
        input.placeholder = t.placeholder;
        botaoEnviar.textContent = t.enviar;
        botoesIdioma.forEach(b => b.classList.toggle('ativo', b.dataset.idioma === idioma));
    }

    function mudarIdioma(novo) {
        if (novo === idioma) return;
        idioma = novo;
        sessionStorage.setItem(CHAVE_IDIOMA, idioma);
        aplicarTextos();
        adicionar('assistente', TEXTOS[idioma].idiomaAlterado);
        input.focus();
    }

    function abrirFechar(aberto) {
        painel.hidden = !aberto;
        sessionStorage.setItem(CHAVE_ABERTO, aberto);
        if (aberto) {
            mensagens.scrollTop = mensagens.scrollHeight;
            input.focus();
        }
    }

    aplicarTextos();
    historico.forEach(m => desenhar(m.tipo, m.texto));
    if (historico.length === 0) {
        desenhar('assistente', TEXTOS[idioma].boasVindas);
    }
    abrirFechar(sessionStorage.getItem(CHAVE_ABERTO) === 'true');

    botoesIdioma.forEach(b => b.addEventListener('click', () => mudarIdioma(b.dataset.idioma)));
    botaoToggle.addEventListener('click', () => abrirFechar(painel.hidden));
    botaoFechar.addEventListener('click', () => abrirFechar(false));
    botaoLimpar.addEventListener('click', () => {
        historico = [];
        sessionStorage.removeItem(CHAVE_HISTORICO);
        mensagens.replaceChildren();
        desenhar('assistente', TEXTOS[idioma].limpo);
    });

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        const pergunta = input.value.trim();
        if (!pergunta) return;

        const contexto = historico
            .filter(m => m.tipo === 'utilizador' || m.tipo === 'assistente')
            .slice(-10)
            .map(m => ({ papel: m.tipo, texto: m.texto }));

        adicionar('utilizador', pergunta);
        input.value = '';
        botaoEnviar.disabled = true;
        const t = TEXTOS[idioma];
        const aPensar = desenhar('assistente', t.aPensar);

        try {
            const r = await fetch('/Assistente/Perguntar', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
                body: JSON.stringify({ pergunta, historico: contexto, idioma })
            });
            const dados = await r.json().catch(() => ({}));
            aPensar.remove();
            if (r.ok && dados.resposta) {
                adicionar('assistente', dados.resposta);
            } else {
                adicionar('erro', r.status === 503 ? t.indisponivel : t.erroResposta);
            }
        } catch {
            aPensar.remove();
            adicionar('erro', t.erroLigacao);
        } finally {
            botaoEnviar.disabled = false;
            input.focus();
        }
    });
})();
