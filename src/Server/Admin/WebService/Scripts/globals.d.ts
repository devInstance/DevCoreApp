declare namespace bootstrap {
    class Modal {
        constructor(element: string | Element, options?: { keyboard?: boolean; focus?: boolean });
        show(): void;
        hide(): void;
        static getInstance(element: Element): Modal | null;
    }
}

declare namespace Blazor {
    function reconnect(): Promise<boolean>;
    function resumeCircuit(): Promise<boolean>;
}

interface Window {
    showBootstrapModal(id: string): boolean;
    dismissBootstrapModal(id: string): boolean;
    downloadFileFromBytes(fileName: string, contentType: string, bytes: ArrayLike<number>): void;
}
