// Máscara monetária estilo "caixa registradora": os dígitos preenchem da direita
// para a esquerda (2 → 0,02 → 0,22 → 2,22). Executa no navegador para evitar
// round-trips do SignalR a cada tecla.
const MAX_DIGITS = 13;
const formatter = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const handlers = new WeakMap();

function format(value) {
    const digits = value.replace(/\D/g, '').replace(/^0+/, '').slice(0, MAX_DIGITS);
    return digits ? formatter.format(Number(digits) / 100) : '';
}

function moveCaretToEnd(input) {
    const end = input.value.length;
    input.setSelectionRange(end, end);
}

export function attach(container) {
    const input = container?.querySelector('input');
    if (!input || handlers.has(container)) return;

    input.setAttribute('inputmode', 'numeric');
    input.setAttribute('autocomplete', 'off');

    const onInput = () => {
        input.value = format(input.value);
        moveCaretToEnd(input);
    };
    const onFocus = () => requestAnimationFrame(() => moveCaretToEnd(input));

    input.addEventListener('input', onInput);
    input.addEventListener('focus', onFocus);
    input.addEventListener('click', onFocus);
    handlers.set(container, { input, onInput, onFocus });
}

export function detach(container) {
    const h = container && handlers.get(container);
    if (!h) return;

    h.input.removeEventListener('input', h.onInput);
    h.input.removeEventListener('focus', h.onFocus);
    h.input.removeEventListener('click', h.onFocus);
    handlers.delete(container);
}
