/**
 * Extract error message from API response
 * Handles various error response formats from the backend
 */
export const extractErrorMessage = (error: any, defaultMessage: string = "An error occurred"): string => {
    // Handle HTTP error responses (400, 500, etc.)
    if (error?.response?.data?.message) {
        return error.response.data.message;
    }

    if (error?.response?.data?.data) {
        return typeof error.response.data.data === 'string'
            ? error.response.data.data
            : (error.response.data.data?.message || JSON.stringify(error.response.data.data));
    }

    if (error?.message) {
        return error.message;
    }

    return defaultMessage;
};

/**
 * Handle API error and return formatted error message
 */
export const handleApiError = (error: any, context: string = "operation"): string => {
    console.error(`Error during ${context}:`, error);
    return extractErrorMessage(error, `Error during ${context}`);
};
